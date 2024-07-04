using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

namespace XRefTool.Controls
{
    public partial class ucParameter : UserControl
    {
        private ParameterInfo _p;
        private NodeContext _context;
        public Type ParameterType => _p.ParameterType;
        public object ParameterValue
        {
            get
            {

                var type = Nullable.GetUnderlyingType(ParameterType) ?? ParameterType;
                var typeCode = Type.GetTypeCode(type);

                switch (typeCode)
                {
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Object:
                        if (type.IsClass)
                        {
                            //var obj = Activator.CreateInstance(ParameterType);
                            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(this.textBox1.Text, type);
                            return obj;
                        }

                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                        break;
                    case TypeCode.Char:
                        break;
                    case TypeCode.SByte:
                        break;
                    case TypeCode.Byte:
                        break;
                    case TypeCode.Int16:
                        break;
                    case TypeCode.UInt16:
                        break;
                    case TypeCode.Int32:
                        break;
                    case TypeCode.UInt32:
                        break;
                    case TypeCode.Int64:
                        break;
                    case TypeCode.UInt64:
                        break;
                    case TypeCode.Single:
                        break;
                    case TypeCode.Double:
                        break;
                    case TypeCode.Decimal:
                        break;
                    case TypeCode.DateTime:
                        break;
                    case TypeCode.String:
                        break;
                    default:
                        if (type.IsClass)
                        {
                            //var obj = Activator.CreateInstance(ParameterType);
                            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(this.textBox1.Text, type);
                            return obj;
                        }
                        break;
                }
                
                return textBox1.Text;
            }
        }
        public ucParameter()
        {
            InitializeComponent();
        }

        public ucParameter(ParameterInfo p, NodeContext context) : this()
        {
            _context = context;
            Add(p);
        }

        public void Add(ParameterInfo p)
        {
            label1.Text = $"{p.Name}";
            label2.Text = $"{p.ParameterType}";
            _p = p;
        }
    }
}
