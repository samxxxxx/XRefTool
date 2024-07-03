using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XRefTool.Controls
{
    public class ucTreeView : TreeView
    {

        public TreeNodeExt Selected
        {
            get
            {
                return SelectedNode as TreeNodeExt;
            }
        }
    }
}
