using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TelloLib;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using System.IO;


/* CONTROL / COORDINATE SYSTEM
 * lx: A, D -> pan left, right
 * ly: W, S -> fly up, down
 * rx: left, right arrow -> fly left, right
 * ry: up, down arrow    -> fly forward, backward
 * 
 * 
 * For head motion to work, press RIGHT CONTROL key to start logging after pressing the Unity Play button
 * 
 */


public class TelloController : SingletonMonoBehaviour<TelloController> {

	public Vector3 drone_position = Vector3.zero;

	static string dataPath = Directory.GetCurrentDirectory() + "/Assets/Logs/";
	string logFile = dataPath + "Data-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
	string csv;

	public EyeTracking_Nik ET;

	private static bool isLoaded = false;

	private TelloVideoTexture telloVideoTexture;

	// FlipType is used for the various flips supported by the Tello.
	public enum FlipType
	{

		// FlipFront flips forward.
		FlipFront = 0,

		// FlipLeft flips left.
		FlipLeft = 1,

		// FlipBack flips backwards.
		FlipBack = 2,

		// FlipRight flips to the right.
		FlipRight = 3,

		// FlipForwardLeft flips forwards and to the left.
		FlipForwardLeft = 4,

		// FlipBackLeft flips backwards and to the left.
		FlipBackLeft = 5,

		// FlipBackRight flips backwards and to the right.
		FlipBackRight = 6,

		// FlipForwardRight flips forewards and to the right.
		FlipForwardRight = 7,
	};

	// VideoBitRate is used to set the bit rate for the streaming video returned by the Tello.
	public enum VideoBitRate
	{
		// VideoBitRateAuto sets the bitrate for streaming video to auto-adjust.
		VideoBitRateAuto = 0,

		// VideoBitRate1M sets the bitrate for streaming video to 1 Mb/s.
		VideoBitRate1M = 1,

		// VideoBitRate15M sets the bitrate for streaming video to 1.5 Mb/s
		VideoBitRate15M = 2,

		// VideoBitRate2M sets the bitrate for streaming video to 2 Mb/s.
		VideoBitRate2M = 3,

		// VideoBitRate3M sets the bitrate for streaming video to 3 Mb/s.
		VideoBitRate3M = 4,

		// VideoBitRate4M sets the bitrate for streaming video to 4 Mb/s.
		VideoBitRate4M = 5,

	};

	override protected void Awake()
	{
		if (!isLoaded) {
			DontDestroyOnLoad(this.gameObject);
			isLoaded = true;
		}
		base.Awake();

		Tello.onConnection += Tello_onConnection;
		Tello.onUpdate += Tello_onUpdate;
		Tello.onVideoData += Tello_onVideoData;

		if (telloVideoTexture == null)
			telloVideoTexture = FindObjectOfType<TelloVideoTexture>();


	}

	private void OnEnable()
	{
		if (telloVideoTexture == null)
			telloVideoTexture = FindObjectOfType<TelloVideoTexture>();
	}

	private void Start()
	{
		if (telloVideoTexture == null)
			telloVideoTexture = FindObjectOfType<TelloVideoTexture>();

		Tello.startConnecting();
		csv = "posX\tposY\tposZ\tvelX\tvelY\tvelZ\tvelN\tvelE\tvelD\n";
		File.WriteAllText(logFile, csv);
	}

	void OnApplicationQuit()
	{
		Tello.stopConnecting();
	}

	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown(KeyCode.T)) {
			Tello.takeOff();
		} else if (Input.GetKeyDown(KeyCode.L)) {
			Tello.land();
		}

		float lx = 0f;
		float ly = 0f;
		float rx = 0f;
		float ry = 0f;

		ry = ET.fb_command;
		lx = ET.rot_command;

		if (Input.GetKey(KeyCode.UpArrow))
		{
			ry = 1;
		}
		if (Input.GetKey(KeyCode.DownArrow))
		{
			ry = -1;
		}
		if (Input.GetKey(KeyCode.D))
		{
			lx = 1;
		}
		if (Input.GetKey(KeyCode.A))
		{
			lx = -1;
		}


		if (Input.GetKey(KeyCode.RightArrow)) {
			rx = 1;
		}
		if (Input.GetKey(KeyCode.LeftArrow)) {
			rx = -1;
		}
		if (Input.GetKey(KeyCode.W)) {
			ly = 1;
		}
		if (Input.GetKey(KeyCode.S)) {
			ly = -1;
		}


        

        
       //  lx = 0;

        Tello.controllerState.setAxis(lx, ly, rx, ry);

		// Debug.Log("Tello_onUpdate : " + Tello.state);
		csv = "";
		csv += Tello.state.posX.ToString();
		csv += "\t";
		csv += Tello.state.posY.ToString();
		csv += "\t";
		csv += Tello.state.posZ.ToString();
		csv += "\t";
		csv += Tello.state.velX.ToString();
		csv += "\t";
		csv += Tello.state.velY.ToString();
		csv += "\t";
		csv += Tello.state.velZ.ToString();
		csv += "\t";
		csv += Tello.state.velN.ToString();
		csv += "\t";
		csv += Tello.state.velE.ToString();
		csv += "\t";
		csv += Tello.state.velD.ToString();
		csv += "\n";
		File.AppendAllText(logFile, csv);

		drone_position.x = Tello.state.posX;
		drone_position.y = Tello.state.posY;
		drone_position.z = Tello.state.posZ;

	}

	private void Tello_onUpdate(int cmdId)
	{
		//throw new System.NotImplementedException();
	}

	private void Tello_onConnection(Tello.ConnectionState newState)
	{
		//throw new System.NotImplementedException();
		//Debug.Log("Tello_onConnection : " + newState);
		if (newState == Tello.ConnectionState.Connected) {
            Tello.queryAttAngle();
            Tello.setMaxHeight(50);

			Tello.setPicVidMode(1); // 0: picture, 1: video
			Tello.setVideoBitRate((int)VideoBitRate.VideoBitRateAuto);
			//Tello.setEV(0);
			Tello.requestIframe();
		}
	}

	private void Tello_onVideoData(byte[] data)
	{
		//Debug.Log("Tello_onVideoData: " + data.Length);
		if (telloVideoTexture != null)
			telloVideoTexture.PutVideoData(data);
	}

}
