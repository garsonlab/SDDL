using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SDDL;

namespace Protocol
{
	public static partial class Const
	{
		public const int Phone_HOME = 1;
		public const int Phone_MOBILE = 0;
		public const int Phone_WORK = 2;
	}
	public class Phone : Protocol.Value
	{
		public string no
		{
			get { return __no__; }
			set { __no__ = value ?? ""; }
		}
		private string __no__ = "";
		public int type;
		public bool Read(Protocol.Format format, int key)
		{
			switch (key)
			{
			case 1:
				return format.ReadValue(ref __no__);
			case 2:
				return format.ReadValue(ref type);
			default:
				return format.Skip();
			}
		}
		public void Write(Protocol.Format format)
		{
			format.WriteValue(1, __no__);
			format.WriteValue(2, type);
		}
		public void Reset()
		{
			__no__ = "";
			type = 0;
		}
		public string Keys(int key)
		{
			switch (key)
			{
			case 1:
				return "no";
			case 2:
				return "type";
			default:
				return null;
			}
		}
		public void Assign(Phone rhs)
		{
			__no__ = rhs.__no__;
			type = rhs.type;
		}
	}
}
