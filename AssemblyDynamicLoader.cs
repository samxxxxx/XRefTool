using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XRefTool
{
    public class AssemblyDynamicLoader
    {
        private AppDomain appDomain;
        private RemoteLoader remoteLoader;
        public AssemblyDynamicLoader(string pluginName)
        {
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationName = "app_" + pluginName;
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            setup.PrivateBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            setup.CachePath = setup.ApplicationBase;
            setup.ShadowCopyFiles = "true";
            setup.ShadowCopyDirectories = setup.ApplicationBase;
            AppDomain.CurrentDomain.SetShadowCopyFiles();
            this.appDomain = AppDomain.CreateDomain("app_" + pluginName, null, setup);

            String name = Assembly.GetExecutingAssembly().GetName().FullName;
            this.remoteLoader = (RemoteLoader)this.appDomain.CreateInstanceAndUnwrap(name, typeof(RemoteLoader).FullName);
        }

        /// <summary>
        /// 加载程序集
        /// </summary>
        /// <param name="assemblyFile"></param>
        public void LoadAssembly(string assemblyFile)
        {
            remoteLoader.LoadAssembly(assemblyFile);
        }

        /// <summary>
        /// 创建对象实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public T GetInstance<T>(string typeName) where T : class
        {
            if (remoteLoader == null) return null;
            return remoteLoader.GetInstance<T>(typeName);
        }

        /// <summary>
        /// 执行类型方法
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public object ExecuteMothod(string className, string methodName, object[] paramsValues)
        {
            return remoteLoader.ExecuteMethod(className, methodName, paramsValues);
        }

        /// <summary>
        /// 卸载应用程序域
        /// </summary>
        public void Unload()
        {
            try
            {
                if (appDomain == null) return;
                AppDomain.Unload(this.appDomain);
                this.appDomain = null;
                this.remoteLoader = null;
            }
            catch (CannotUnloadAppDomainException ex)
            {
                throw ex;
            }
        }

        public Assembly[] GetAssemblies()
        {
            return this.appDomain.GetAssemblies();
        }

        public void SetConfig(string webconfig)
        {
            remoteLoader.SetConfig(webconfig);
        }
    }

    public class RemoteLoader : MarshalByRefObject
    {
        private Assembly _assembly;
        private string _webConfigPath;

        /// <summary>
        /// 设置AppDomain环境的web.config或app.config路径
        /// </summary>
        /// <param name="webconfig"></param>
        public void SetConfig(string webconfig)
        {
            _webConfigPath = webconfig;
        }

        public void LoadAssembly(string assemblyFile)
        {
            try
            {
                _assembly = Assembly.LoadFrom(assemblyFile);
                //return _assembly;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public T GetInstance<T>(string typeName) where T : class
        {
            if (_assembly == null) return null;
            var type = _assembly.GetType(typeName);
            if (type == null) return null;
            return Activator.CreateInstance(type) as T;
        }

        //public object ExecuteMothod(string typeName, string args)
        //{
        //    if (_assembly == null) return null;
        //    var type = _assembly.GetType(typeName);
        //    var obj = Activator.CreateInstance(type);
        //    if (obj is IPlugin)
        //    {
        //        return (obj as IPlugin).Exec(args);
        //    }
        //    return null;
        //}

        public object ExecuteMethod(string className, string methodName, object[] paramsValues)
        {
            try
            {
                if (_assembly == null) return null;
                
                if (AppDomain.CurrentDomain.SetupInformation.ConfigurationFile != _webConfigPath)
                {
                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", _webConfigPath);
                }

                var type = _assembly.GetType(className);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);
                var method = methods.FirstOrDefault(x => x.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
                var instance = Activator.CreateInstance(type);

                return method.Invoke(instance, paramsValues);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
