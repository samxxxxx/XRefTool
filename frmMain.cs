﻿using System.Text.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using XRefTool.Controls;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace XRefTool
{
    /*
     图标地址：https://www.cnblogs.com/q787011187/p/16455035.html
     https://learn.microsoft.com/zh-cn/previous-versions/visualstudio/visual-studio-2017/ide/class-view-and-object-browser-icons?view=vs-2017&viewFallbackFrom=vs-2019
     https://learn.microsoft.com/zh-cn/visualstudio/extensibility/ux-guidelines/visual-language-dictionary-for-visual-studio?view=vs-2022#BKMK_VLDProducts
         */
    public partial class frmMain : Form
    {
        /// <summary>
        /// 
        /// </summary>
        public frmMain()
        {
            InitializeComponent();
            dataGridView1.AutoGenerateColumns = false;
            LoadDLL(AppDomain.CurrentDomain.BaseDirectory);

#if DEBUG
            txtConfig.Text = @"C:\Users\xiao_wu\Documents\Visual Studio 2015\Projects\WindowsFormsApplication1\XRefTool\bin\Debug\Web.config";
#endif
        }

        private void LoadDLL(string path)
        {
            var files = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);

            var fdata = files.Select(x => new FileData { DllPath = x, Name = Path.GetFileName(x) }).ToList();
            dataGridView1.DataSource = fdata;
            dataGridView1.Refresh();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (!_isload) return;
            if (dataGridView1.SelectedRows.Count > 0)
            {
                var item = dataGridView1.SelectedRows[0].DataBoundItem as FileData;

                //清除记录
                ucTreeView1.Nodes.Clear();
                LoadMethod(item);
            }
        }

        private void LoadMethod(FileData fileData)
        {
            var node = AddOrGetTreeNode(fileData.Name, ucTreeView1.Nodes, DllDataTypeEnum.根节点文件名, fileData);
            var ass = Assembly.LoadFrom(fileData.DllPath);
            var types = ass.GetTypes();

            node.ImageIndex = 2;
            node.Context.Assembly = ass;
            node.Nodes.Clear();

            foreach (var type in types.OrderBy(x => x.FullName))
            {
                if (type.IsClass && type.IsPublic)
                {
                    var classNode = AddOrGetTreeNode(type.FullName, node.Nodes, DllDataTypeEnum.类名节点, fileData);
                    classNode.ImageIndex = 3;
                    classNode.Context.Assembly = ass;
                    classNode.Context.ClassName = type.FullName;
                    classNode.ExpandAll();
                }
            }
            node.ExpandAll();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="nodes"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private TreeNodeExt AddOrGetTreeNode(string text, TreeNodeCollection nodes, DllDataTypeEnum type, object dataTag = null)
        {
            TreeNodeExt isFind = null;
            foreach (TreeNodeExt node in nodes)
            {
                var data = node.Tag as DllData;
                if (data != null && data.Type == type && data.FileName == text)
                {
                    isFind = node;
                    break;
                }
            }

            if (isFind == null)
            {
                isFind = new TreeNodeExt();
                isFind.Text = text;
                isFind.Context.Type = type;
                var tag = new DllData
                {
                    Type = type,
                    FileName = text,
                    Tag = dataTag
                };
                isFind.Tag = tag;

                if (isFind.Parent != null)
                {

                }

                int i = nodes.Add(isFind);
                isFind.ExpandAll();
            }
            return isFind;
        }

        private bool _isload = false;
        private void Form1_Load(object sender, EventArgs e)
        {
            _isload = true;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var data = e.Node.Tag as DllData;
            if (data == null)
                return;

            if (data.Type == DllDataTypeEnum.类名节点)
            {
                e.Node.Nodes.Clear();

                var tree = e.Node as TreeNodeExt;

                var type = tree.Context.Assembly.GetType(tree.Context.ClassName, true);
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase);

                foreach (var method in methods.OrderBy(x => x.Name))
                {
                    var parentNodeContext = (e.Node as TreeNodeExt).Context;
                    var node = AddOrGetTreeNode(method.Name, e.Node.Nodes, DllDataTypeEnum.方法名节点, method);
                    node.Context.Assembly = parentNodeContext.Assembly;
                    node.Context.ClassName = parentNodeContext.ClassName;
                    node.Context.MethodInfo = method;
                    node.ImageIndex = 1;
                    node.ExpandAll();
                }
            }
            else if (data.Type == DllDataTypeEnum.方法名节点)
            {
                var context = (e.Node as TreeNodeExt).Context.Clone() as NodeContext;
                var ps = context.MethodInfo.GetParameters();

                foreach (var p in ps)
                {
                    var node = AddOrGetTreeNode($"{p.Name}({p.ParameterType.FullName})", e.Node.Nodes, DllDataTypeEnum.参数节点, p);
                    node.Context = context;
                    node.ImageIndex = 4;
                    node.Context.Type = DllDataTypeEnum.参数节点;
                }

                AddParameter(ps, context);
            }
            e.Node.ExpandAll();

            ucTreeView1.SelectedImageIndex = ucTreeView1.SelectedNode.ImageIndex;
        }

        private void AddParameter(ParameterInfo[] ps, NodeContext context)
        {
            flowLayoutPanel1.Controls.Clear();
            foreach (var p in ps)
            {

                flowLayoutPanel1.Controls.Add(new ucParameter(p, context));

            }
        }

        /// <summary>
        /// 获取方法的所有参数类型
        /// </summary>
        /// <returns></returns>
        private Type[] GetParameterType()
        {
            var pCount = flowLayoutPanel1.Controls.Count;

            Type[] params_type = new Type[pCount];
            for (int i = 0; i < flowLayoutPanel1.Controls.Count; i++)
            {
                var ctl = flowLayoutPanel1.Controls[i] as ucParameter;
                params_type[i] = ctl.ParameterType;
            }
            return params_type;
        }

        /// <summary>
        /// 返回方法的所有参数类型值
        /// </summary>
        /// <returns></returns>
        private object[] GetParameterValue()
        {
            var pCount = flowLayoutPanel1.Controls.Count;
            object[] params_obj = new Object[pCount];
            for (int i = 0; i < flowLayoutPanel1.Controls.Count; i++)
            {
                var ctl = flowLayoutPanel1.Controls[i] as ucParameter;
                params_obj[i] = ctl.ParameterValue;
            }
            return params_obj;
        }

        /// <summary>
        /// 调用执行dll方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            var types = GetParameterType();
            var values = GetParameterValue();

            var node = ucTreeView1.Selected;
            if (node != null && node.Context.Type == DllDataTypeEnum.方法名节点)
            {
                object res = null;
                AssemblyDynamicLoader loader = null;
                try
                {
                    loader = new AssemblyDynamicLoader(node.Context.ClassName, txtConfig.Text);
                    listBox1.Items.Clear();
                    if (menuAtta.HasDropDownItems)
                    {
                        foreach (ToolStripMenuItem item in menuAtta.DropDownItems)
                        {
                            if (item.Checked)
                                loader.LoadAssembly(item.Text);
                        }
                    }
                    loader.LoadAssembly(node.Context.Assembly.Location);
                    res = loader.ExecuteMothod(node.Context.ClassName, node.Context.MethodInfo.Name, values);
                    foreach (var item in loader.GetAssemblies())
                    {
                        listBox1.Items.Add($"已加载“{item.FullName}”");
                    }
                    if (res != null)
                    {
                        if (res.GetType() == typeof(CustomSerializableObjet))
                        {
                            var cs = res as CustomSerializableObjet;
                            res = cs.GetOriginalObject();
                        }
                        else
                        {
                            var list = res as List<CustomSerializableObjet>;
                            if (list != null)
                            {
                                res = list.Select(x => x.GetOriginalObject()).ToList();
                            }
                        }
                    }
                    txtResult.Text = JsonSerializer.Serialize(res, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                    });
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        txtResult.Text = ex.ToString();
                    }
                    else
                    {
                        txtResult.Text = $"系统错误：\r\n{ex.ToString()}";
                    }
                }
                finally
                {
                    loader.Unload();
                }
            }
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtConfig.Text = dlg.FileName;
            }
        }

        private void menuAtta_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(dlg.FileName);
                item.Checked = true;
                item.CheckOnClick = true;
                item.CheckState = CheckState.Checked;
                menuAtta.DropDownItems.Add(item);
            }

        }

        private void mnuOpenDir_Click(object sender, EventArgs e)
        {
            var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LoadDLL(dlg.SelectedPath);
            }
        }
    }

    public class FileData
    {
        public string DllPath { get; set; }
        public string Name { get; set; }
    }

    public class DllData
    {
        public string FileName { get; set; }
        public DllDataTypeEnum Type { get; set; }
        public object Tag { get; set; }
    }

    public enum DllDataTypeEnum
    {
        根节点文件名,
        类名节点,
        方法名节点,
        参数节点
    }

    public class NodeContext : ICloneable
    {
        public Assembly Assembly { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public string ClassName { get; set; }
        public DllDataTypeEnum Type { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class TreeNodeExt : TreeNode
    {
        public TreeNodeExt()
        {
            Context = new NodeContext();
        }
        public NodeContext Context { get; set; }
    }
}
