using System.Text.Json;
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
                            if (string.IsNullOrEmpty(textBox1.Text)) return null;
                            var obj = JsonSerializer.Deserialize(this.textBox1.Text, type);
                            return obj;
                        }

                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                    case TypeCode.DateTime:
                    case TypeCode.String:
                        if(type.BaseType == typeof(Enum)|| type == typeof(Enum))
                        {
                            if (!Enum.IsDefined (type, Convert.ToInt32( textBox1.Text)))
                            {
                                return 0;
                            }
                            return Enum.Parse(type, textBox1.Text);
                        }
                        var value = Convert.ChangeType(textBox1.Text, type);
                        return value;
                        break;
                    default:
                        if (type.IsClass)
                        {
                            if (string.IsNullOrEmpty(textBox1.Text)) return null;
                            var obj = JsonSerializer.Deserialize(this.textBox1.Text, type);
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
