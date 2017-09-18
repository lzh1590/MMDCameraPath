using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VMDCameraWork : MonoBehaviour
{

	public string CameraVMDFilePath;
	public Animator ModelAnimator;
	public float VmdToUnityCameraViewScale = 1f;//1.142857f;

	private VMDCameraForm mVmdCameraForm;
	private VMD_CAMERA[] mVmdCameras;
	private float VmdToUnityVectorScale = 0.125f;
	private Camera mCamera;
	private float cliptimelength = 218.067f;

	private int vmdflamoscale = 30;//这个就是fps，mmd导入unity，动作锁了30fps，没办法,用来在update里用过时间判断现在是多少帧

	// 在unity3d里，导入fbx的动画是一秒走30帧
	void Awake ()
	{
		mCamera = GetComponentInChildren<Camera> ();
		points = new float[6];
	}
	// Use this for initialization
	void Start ()
	{

		mVmdCameraForm = new VMDCameraForm (Application.dataPath + "/" + CameraVMDFilePath);
		mVmdCameras = mVmdCameraForm.mVmdCameras;
		//数据微调，使mikudangce的坐标系符合unity3d的坐标系
		for (int i = 0; i < mVmdCameras.Length; i++) {
			//坐标系缩放
			mVmdCameras [i].length *= VmdToUnityVectorScale;
			for (int l = 0; l < 3; l++) {
				mVmdCameras [i].Location [l] *= VmdToUnityVectorScale;
			}
		}
		//初始化数据
		//设置length
		mCamera.transform.localPosition = new Vector3 (0, 0, mVmdCameras [0].length);

		//设置fov
		mCamera.fieldOfView = mVmdCameras [0].ViewingAngle / VmdToUnityCameraViewScale;
		//中心点坐标及旋转设置
		transform.localPosition = new Vector3 (mVmdCameras [0].Location [0], mVmdCameras [0].Location [1], mVmdCameras [0].Location [2]);
		float rx = (-1) * mVmdCameras [0].Rotate [0] * 180 / Mathf.PI;
		float ry = (-1) * mVmdCameras [0].Rotate [1] * 180 / Mathf.PI;
		float rz = (-1) * mVmdCameras [0].Rotate [2] * 180 / Mathf.PI;
		transform.localRotation = Quaternion.Euler (rx, ry, rz);//用角度重置rotate
	}

	private int cameraPos = 0;
	private float mFlamoNo = 0;
	private float[] points;
	//x,y,z,R,L,View
	// Update is called once per frame
	void Update ()
	{
		mFlamoNo = (float)ModelAnimator.GetTime () * vmdflamoscale;
		if (cameraPos < mVmdCameras.Length - 2 && mFlamoNo > mVmdCameras [cameraPos + 1].FlameNo)
			cameraPos++;
		if (cameraPos < mVmdCameras.Length - 2 && mFlamoNo < mVmdCameras [cameraPos + 1].FlameNo) {
			//计算当前帧插值t
			float t = (mFlamoNo - mVmdCameras [cameraPos].FlameNo) / (mVmdCameras [cameraPos + 1].FlameNo - mVmdCameras [cameraPos].FlameNo);
			for (int i = 0; i < 6; i++) {
				Vector3 p1 = new Vector3 (mVmdCameras [cameraPos].Interpolation [i * 4], mVmdCameras [cameraPos].Interpolation [i * 4 + 2]);	
				Vector3 p2 = new Vector3 (mVmdCameras [cameraPos].Interpolation [i * 4 + 1], mVmdCameras [cameraPos].Interpolation [i * 4 + 3]);
				points [i] = CalculBezierPointByTwo (t, p1, p2);
			}
			float x = mVmdCameras [cameraPos].Location [0] + points [0] * (mVmdCameras [cameraPos + 1].Location [0] - mVmdCameras [cameraPos].Location [0]);
			float y = mVmdCameras [cameraPos].Location [1] + points [1] * (mVmdCameras [cameraPos + 1].Location [1] - mVmdCameras [cameraPos].Location [1]);
			float z = mVmdCameras [cameraPos].Location [2] + points [2] * (mVmdCameras [cameraPos + 1].Location [2] - mVmdCameras [cameraPos].Location [2]);
			transform.localPosition = new Vector3 (x, y, z);
			float rx = mVmdCameras [cameraPos].Rotate [0] * 180 / Mathf.PI + points [3] * (mVmdCameras [cameraPos + 1].Rotate [0] - mVmdCameras [cameraPos].Rotate [0]);
			float ry = mVmdCameras [cameraPos].Rotate [1] * 180 / Mathf.PI + points [3] * (mVmdCameras [cameraPos + 1].Rotate [1] - mVmdCameras [cameraPos].Rotate [1]);
			float rz = mVmdCameras [cameraPos].Rotate [2] * 180 / Mathf.PI + points [3] * (mVmdCameras [cameraPos + 1].Rotate [2] - mVmdCameras [cameraPos].Rotate [2]);
			transform.localRotation = Quaternion.Euler ((-1) * rx, (-1) * ry, (-1) * rz);//用角度重置rotate
			float l = mVmdCameras [cameraPos].length + points [4] * (mVmdCameras [cameraPos + 1].length - mVmdCameras [cameraPos].length);
			mCamera.transform.localPosition = new Vector3 (0, 0, l);
			float view;
			//规避原始数据uint相减出负数错误的情况
			if (mVmdCameras [cameraPos + 1].ViewingAngle > mVmdCameras [cameraPos].ViewingAngle) {
				view = mVmdCameras [cameraPos].ViewingAngle + (mVmdCameras [cameraPos + 1].ViewingAngle - mVmdCameras [cameraPos].ViewingAngle) * points [5];
			} else {
				view = mVmdCameras [cameraPos].ViewingAngle - (mVmdCameras [cameraPos].ViewingAngle - mVmdCameras [cameraPos + 1].ViewingAngle) * points [5];
			}
			mCamera.fieldOfView = view / VmdToUnityCameraViewScale;
		} else if (cameraPos + 1 == mVmdCameras.Length - 2) {
			if (ModelAnimator.GetCurrentAnimatorStateInfo (0).normalizedTime >= 1.0f)
				Application.Quit();//自动结束不起作用，原因在探索，预计是在多线程这里除了问题
		}
		if (Input.GetKeyDown (KeyCode.Escape)) {
			Application.Quit();
		}
	}
	private static float  CalculBezierPointByTwo (float t, Vector3 p1, Vector3 p2)
	{
		float a;
		Vector3 p = CalculateBezierPoint (t, Vector3.zero, p1, p2, new Vector3 (127, 127, 0));
		a = p.y / 127.0f;
		return a;
	}
	private static Vector3 CalculateBezierPoint (float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		float u = 1 - t;
		float tt = t * t;
		float uu = u * u;
		float uuu = uu * u;
		float ttt = tt * t;
		Vector3 p = uuu * p0; //first term
		p += 3 * uu * t * p1; //second term
		p += 3 * u * tt * p2; //third term
		p += ttt * p3; //fourth term
		return p;
	}
}
