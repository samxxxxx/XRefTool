using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace XRefTool
{
    [Serializable]
    public class AssemblyDynamicLoader
    {
        private AppDomain _appDomain;
        private RemoteLoader _remoteLoader;
        private AppDomainSetup _setup;
        private string _pluginName;

        public AssemblyDynamicLoader(string pluginName, string config)
        {
            _pluginName = pluginName;
            _setup = new AppDomainSetup();
            _setup.ApplicationName = "app_" + pluginName;
            _setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            _setup.PrivateBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            _setup.CachePath = _setup.ApplicationBase;
            _setup.ShadowCopyFiles = "true";
            _setup.ShadowCopyDirectories = _setup.ApplicationBase;

            //设置AppDomain环境的web.config或app.config路径
            _setup.ConfigurationFile = config;

            CreateDomain();
        }

        private void CreateDomain()
        {
            this._appDomain = AppDomain.CreateDomain("app_" + _pluginName, null, _setup);
            String name = Assembly.GetExecutingAssembly().GetName().FullName;
            this._remoteLoader = (RemoteLoader)this._appDomain.CreateInstanceAndUnwrap(name, typeof(RemoteLoader).FullName);
        }

        /// <summary>
        /// 加载程序集
        /// </summary>
        /// <param name="assemblyFile"></param>
        public void LoadAssembly(string assemblyFile)
        {
            _remoteLoader.LoadAssembly(assemblyFile);
        }

        /// <summary>
        /// 创建对象实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public T GetInstance<T>(string typeName) where T : class
        {
            if (_remoteLoader == null) return null;
            return _remoteLoader.GetInstance<T>(typeName);
        }

        /// <summary>
        /// 执行类型方法
        /// </summary>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public object ExecuteMothod(string className, string methodName, object[] paramsValues)
        {
            return _remoteLoader.ExecuteMethod(className, methodName, paramsValues);
        }

        /// <summary>
        /// 卸载应用程序域
        /// </summary>
        public void Unload()
        {
            try
            {
                if (_appDomain == null) return;
                AppDomain.Unload(this._appDomain);
                this._appDomain = null;
                this._remoteLoader = null;
            }
            catch (CannotUnloadAppDomainException ex)
            {
                throw ex;
            }
        }

        public Assembly[] GetAssemblies()
        {
            return this._appDomain.GetAssemblies();
        }
    }

    [Serializable]
    public class RemoteLoader : MarshalByRefObject
    {
        private Assembly _assembly;
        public RemoteLoader()
        {

        }

        public void LoadAssembly(string assemblyFile)
        {
            try
            {
                _assembly = Assembly.LoadFrom(assemblyFile);
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

        public object ExecuteMethod(string className, string methodName, object[] paramsValues)
        {
            try
            {
                if (_assembly == null) return null;

                var type = _assembly.GetType(className);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);
                var method = methods.FirstOrDefault(x => x.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
                if (type.IsAbstract && type.IsSealed && type.IsClass && type.GetConstructor(Type.EmptyTypes) == null && method.IsStatic)
                {
                    //静态类直接调用
                    var obj = method.Invoke(null, paramsValues);
                    var arrayObj = obj as IEnumerable<object>;
                    if (arrayObj != null)
                    {
                        var listObj = new List<CustomSerializableObjet>();
                        foreach (var item in arrayObj)
                        {
                            listObj.Add(new CustomSerializableObjet(item));
                        }
                        return listObj;
                    }
                    return new CustomSerializableObjet(obj);
                }
                else
                {
                    var instance = Activator.CreateInstance(type);

                    return method.Invoke(instance, paramsValues);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    [Serializable]
    public class CustomSerializableObjet : ISerializable
    {
        private readonly object _originalObject;
        private readonly Type _originalType;
        public CustomSerializableObjet(object obj)
        {
            _originalObject = obj;
            _originalType = obj.GetType();
        }

        protected CustomSerializableObjet(SerializationInfo info, StreamingContext context)
        {
            _originalType = Type.GetType(info.GetString("OriginalType"));
            _originalObject = Activator.CreateInstance(_originalType);
            foreach (var field in _originalType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object fieldValue = info.GetValue(field.Name, typeof(object));
                field.SetValue(_originalObject, fieldValue);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("OriginalType", _originalType.AssemblyQualifiedName);
            foreach (var field in _originalType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                info.AddValue(field.Name, field.GetValue(_originalObject));
            }
        }

        public object GetOriginalObject()
        {
            return _originalObject;
        }
    }
}
