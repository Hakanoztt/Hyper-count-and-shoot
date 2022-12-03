using UnityEngine;

namespace Mobge.Core
{
    public class EnumFlagAttribute : PropertyAttribute
    {
        public int i_Column;
        public EnumFlagAttribute(int iColumn = 1)
        {
            this.i_Column = iColumn;
        }
    }
}