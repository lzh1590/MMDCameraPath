using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

public class VMDCameraForm
{
	public VMD_HEADER mVmdHeader;
	public VMD_CAMERA[] mVmdCameras;

	private Stream stream;
	public VMDCameraForm(string path){
		LoadVMDToStream (path);
		ParseVMDHeader ();
		ParseVMDCamera ();
		SortFlamoNo ();
	}

	public void LoadVMDToStream (string path)
	{
		stream = File.OpenRead (path);
	}

	#region

	//该函数还要补充模型动作与镜头动作的判断
	public void ParseVMDHeader ()
	{
		if (stream == null)
			return;
		mVmdHeader = new VMD_HEADER ();
		Byte[] buffer = new byte[Marshal.SizeOf (typeof(VMD_HEADER))];
		stream.Read (buffer, 0, buffer.Length);
		mVmdHeader = (VMD_HEADER)RawDeserialize (buffer, typeof(VMD_HEADER));
		if (CompareTwoBytes (mVmdHeader.VmdModelName, mVmdModelName [0])) {
			Console.Write("加载的是模型的动作文件，请更换为镜头动作");
			stream = null;
		}
	}
	#endregion

	public void ParseVMDCamera ()
	{
		if (stream == null)
			return;
		Byte[] buffer = new byte[Marshal.SizeOf (typeof(VMD_DWORD))];
		stream.Read (buffer, 0, buffer.Length);
		stream.Read (buffer, 0, buffer.Length);
		stream.Read (buffer, 0, buffer.Length);
		uint num = BitConverter.ToUInt32 (buffer, 0);
		mVmdCameras = new VMD_CAMERA[num];
		buffer = new byte[Marshal.SizeOf (typeof(VMD_CAMERA))];
		for (int i = 0; i < num; i++) {
			stream.Read (buffer, 0, Marshal.SizeOf (typeof(VMD_CAMERA)));
			mVmdCameras [i] = (VMD_CAMERA)RawDeserialize (buffer, typeof(VMD_CAMERA));
		}
		Console.Write("The camera's number is ：" + num);
	}
	private static bool CompareTwoBytes (byte[] a, byte[] b)
	{
		if (a == null || b == null)
			return false;
		if (a.Length != b.Length)
			return false;
		return string.Compare (Convert.ToBase64String (a), Convert.ToBase64String (b), false) == 0 ? true : false;
	}
	private void SortFlamoNo(){
		for (int i = 0; i < mVmdCameras.Length - 1; i++)  
		{  
			for (int j = 0; j < mVmdCameras.Length - 1 - i; j++)  
			{  
				if (mVmdCameras[j].FlameNo > mVmdCameras[j + 1].FlameNo)//从大到小排序就用"<"号；反之用">"号  
				{  
					VMD_CAMERA temp = mVmdCameras[j];  
					mVmdCameras[j] = mVmdCameras[j + 1];  
					mVmdCameras[j + 1] = temp;  
				}  
			}  
		} 
	}
	private static object RawDeserialize (byte[] rawdatas, Type anytype)
	{
		int rawsize = Marshal.SizeOf (anytype);
		if (rawsize > rawdatas.Length)
			return null;
		IntPtr buffer = Marshal.AllocHGlobal (rawsize);
		Marshal.Copy (rawdatas, 0, buffer, rawsize);
		object retobj = Marshal.PtrToStructure (buffer, anytype);
		Marshal.FreeHGlobal (buffer);
		return retobj;
	}

	private readonly byte[][] mVmdModelName = new byte[2][] {
		new byte[20] {0x82, 0xB5, 0x82, 0xDC, 0x82, 0xA9, 0x82, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
			, 0x00, 0x00, 0x00, 0x00
		},//骨骼数据
		new byte[20] {0x83, 0x4A, 0x83, 0x81, 0x83, 0x89, 0x81, 0x45, 0x8F, 0xC6, 0x96, 0xBE, 0x00, 0x6F, 0x6E, 0x20
			, 0x44, 0x61, 0x74, 0x61
		}//镜头数据
	};
}
[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
struct VMD_DWORD
{
	public uint Count;
}
[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct VMD_HEADER
{
	[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 30)]
	public string VmdHeader;
	[MarshalAs (UnmanagedType.ByValArray, SizeConst = 20)]
	public byte[] VmdModelName;
	//	因为日文编码未知，不能转换成string，反而会使该值为空，呵呵哒
	//	[MarshalAs(UnmanagedType.ByValTStr,SizeConst = 20)]
	//	public string VmdModelName;
}
[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct VMD_CAMERA
{
	public uint FlameNo;
	public float length;
	[MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
	public float[] Location;
	[MarshalAs (UnmanagedType.ByValArray, SizeConst = 3)]
	public float[] Rotate;
	[MarshalAs (UnmanagedType.ByValArray, SizeConst = 24)]
	public byte[] Interpolation;
	public uint ViewingAngle;
	public byte perspective;
}
 