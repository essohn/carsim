using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class SimManager : MonoBehaviour {

	public enum WeightConfig
	{
		IC, EV, Count
	}

	public enum CameraMode
	{
		HELI, BEHIND, SIDE, Count
	}

	public WeightConfig weightConfig;
	public CameraMode cameraMode;

	public Camera mainCamera;

	public GameObject vehicleObject;
	public GameObject SpawnPoint;

	public GameObject batteryPack;
	public GameObject engineBlock;

	public Transform vehicleT;
	public Rigidbody vehicleBody;

	public Transform cameraTargetT;
	public Transform heliCameraT;
	public Transform sideCameraT;
	public Transform behindCameraT;

	public WheelCollider[] wheelColliders;

	WheelFrictionCurve fcurve = new WheelFrictionCurve();
	WheelFrictionCurve scurve = new WheelFrictionCurve();

	public InputField forwExtSlip;
	public InputField forwExtValue;
	public InputField forwAsymSlip;
	public InputField forwAsymValue;
	public InputField forwStiffness;

	public InputField sideExtSlip;
	public InputField sideExtValue;
	public InputField sideAsymSlip;
	public InputField sideAsymValue;
	public InputField sideStiffness;

	public Text speedText;
	public Text weightModeText;

	private Vector3 refVelocity = Vector3.one;

	public GraphOverlay graphOverlay;

	public SimState simState;

	private float _simTimer;
	public float simTimer { get { return _simTimer; } }

	private float _turnTimer;

	private float _restTimer;

	private bool _isSimStarted;
	private int _simConfigIndex;

	public enum SimState { ACC, TURN, REST, END }

	public string configName;

	public float turnSpeed;

	public float turnTime;

	public float restTime;

	public float driveForce;

	private List<SimConfigTurn> _currentTurns;

	private int _currentTurnIndex;


	[System.Serializable]
	public class SimConfigTurn {

		public float angle;
		public float duration;
	}

	[System.Serializable]
	public class SimConfig {

		public string configName;

		public SimManager.WeightConfig weightConfig;

		public float initSpeed;

		public List<SimConfigTurn> turns;

	}

	public List<SimConfig> simConfigList;


	// Use this for initialization
	void Start () {

		cameraMode = CameraMode.HELI;
		weightConfig = WeightConfig.IC;

		UpdateWeightConfig ();
		UpdateCameraMode ();

		fcurve.extremumSlip = PlayerPrefs.GetFloat ("forwExtSlip", 0.3f);
		fcurve.extremumValue = PlayerPrefs.GetFloat ("forwExtValue", 1f);
		fcurve.asymptoteSlip = PlayerPrefs.GetFloat ("forwAsymSlip", 0.2f);
		fcurve.asymptoteValue = PlayerPrefs.GetFloat ("forwAsymValue", 0.8f);
		fcurve.stiffness = PlayerPrefs.GetFloat ("forwStiffness", 1f);

		scurve.extremumSlip = PlayerPrefs.GetFloat ("sideExtSlip", 0.05f);
		scurve.extremumValue = PlayerPrefs.GetFloat ("sideExtValue", 2f);
		scurve.asymptoteSlip = PlayerPrefs.GetFloat ("sideAsymSlip", 0.02f);
		scurve.asymptoteValue = PlayerPrefs.GetFloat ("sideAsymValue", 1f);
		scurve.stiffness = PlayerPrefs.GetFloat ("sideStiffness", 1f);

		forwExtSlip.text = fcurve.extremumSlip.ToString();
		forwExtValue.text = fcurve.extremumValue.ToString();
		forwAsymSlip.text = fcurve.asymptoteSlip.ToString();
		forwAsymValue.text = fcurve.asymptoteValue.ToString();
		forwStiffness.text = fcurve.stiffness.ToString();

		sideExtSlip.text = scurve.extremumSlip.ToString();
		sideExtValue.text = scurve.extremumValue.ToString();
		sideAsymSlip.text = scurve.asymptoteSlip.ToString();
		sideAsymValue.text = scurve.asymptoteValue.ToString();
		sideStiffness.text = scurve.stiffness.ToString();

	}

	void FixedUpdate() {

		if (_isSimStarted) {

			float vel = (vehicleObject.GetComponent<Rigidbody> ().velocity.magnitude * 3600f / 1000f);

			if (simState == SimState.ACC) {

				if (vel >= turnSpeed) {

					foreach (WheelCollider wheel in wheelColliders) {
						wheel.motorTorque = 0;
						wheel.steerAngle = 0;
						wheel.brakeTorque = 0;
					}

					simState = SimState.TURN;
					_turnTimer = 0;

				} else {

					foreach (WheelCollider wheel in wheelColliders) {

						wheel.steerAngle = 0;
						wheel.brakeTorque = 0;

						if (wheel.transform.localPosition.z < 0) {
							wheel.motorTorque = driveForce;
						}

						if (wheel.transform.localPosition.z >= 0) {
							wheel.motorTorque = driveForce;
						}
					}

				}

			} else if (simState == SimState.TURN) {

				foreach (WheelCollider wheel in wheelColliders) {
					wheel.motorTorque = 0;
					wheel.brakeTorque = 0;

					if (wheel.transform.localPosition.z > 0)
						wheel.steerAngle = _currentTurns[_currentTurnIndex].angle;
				}

				if (_turnTimer >= _currentTurns[_currentTurnIndex].duration) {

					_currentTurnIndex++;
					_turnTimer = 0;

					if (_currentTurnIndex >= _currentTurns.Count) {
						simState = SimState.REST;
						_restTimer = 0;
					}
				}

				_turnTimer += Time.deltaTime;

			} else if (simState == SimState.REST) {

				foreach (WheelCollider wheel in wheelColliders) {
					wheel.motorTorque = 0;
					wheel.brakeTorque = 1000000;
					wheel.steerAngle = 0;
				}

				if (_restTimer >= restTime) {
					simState = SimState.END;

					SaveFile ();
				}

				_restTimer += Time.deltaTime;

			} else if (simState == SimState.END) {


				if (_simConfigIndex + 1 >= simConfigList.Count) {
					_isSimStarted = false;
				} else {
					StartSimIndex (_simConfigIndex + 1);
				}
			}


			_simTimer += Time.deltaTime;
		}

		UpdateCameraMode ();

	}
	
	// Update is called once per frame
	void Update () {

		// selecting weight mode

		if (Input.GetKeyDown (KeyCode.W)) {
			weightConfig ++;
			if (weightConfig >= WeightConfig.Count)
				weightConfig = 0;

			UpdateWeightConfig ();
		}

		if (Input.GetKeyDown (KeyCode.C)) {
			cameraMode ++;
			if (cameraMode >= CameraMode.Count)
				cameraMode = 0;

		}

		if (Input.GetKeyDown (KeyCode.R)) {
			ResetCar ();
		}


		speedText.text = ((int)(vehicleObject.GetComponent<Rigidbody> ().velocity.magnitude * 3600f / 1000f )).ToString() + " km/h";
		weightModeText.text = "Weight config: " + weightConfig.ToString ();
	}


	void ResetCar() {
		
		vehicleT.rotation = Quaternion.identity;
		vehicleT.position = SpawnPoint.transform.position;
		vehicleObject.GetComponent<Rigidbody> ().velocity = Vector3.zero;

		StartCoroutine ( SleepAndWakeUp(1) );

	}

	IEnumerator SleepAndWakeUp(float time) {
		vehicleObject.GetComponent<Rigidbody> ().Sleep ();
		yield return new WaitForSeconds (time);
		vehicleObject.GetComponent<Rigidbody> ().WakeUp ();
	}

	public void UpdateWeightConfig() {

		if (weightConfig == WeightConfig.EV) {

			batteryPack.SetActive (true);
			engineBlock.SetActive (false);

		} else if (weightConfig == WeightConfig.IC) {
			
			batteryPack.SetActive (false);
			engineBlock.SetActive (true);
		}
	}

	public void UpdateCameraMode() {


		Vector3 curCamPos = mainCamera.transform.position;

		Vector3 heliPos = heliCameraT.position;

		if (heliPos.y < 0)
			heliPos.y = -heliPos.y;
		
		if (cameraMode == CameraMode.HELI) {
			curCamPos = Vector3.SmoothDamp (curCamPos, heliPos, ref refVelocity, 0.3f);
		} else if (cameraMode == CameraMode.SIDE) {
			curCamPos = Vector3.SmoothDamp (curCamPos, sideCameraT.position, ref refVelocity, 0.3f);
		} else if (cameraMode == CameraMode.BEHIND) {
			curCamPos = Vector3.SmoothDamp (curCamPos, behindCameraT.position, ref refVelocity, 0.3f);
		}


		mainCamera.transform.position = curCamPos;

		mainCamera.transform.LookAt (cameraTargetT.position);
	}

	public void OnUpdateFrontExtremumSlip(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		fcurve.extremumSlip = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateFrontExtremumValue(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		fcurve.extremumValue = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateFrontAsymptoteSlip(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		fcurve.asymptoteSlip = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateFrontAsymptoteValue(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		fcurve.asymptoteValue = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateFrontStiffness(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		fcurve.stiffness = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateSideExtremumSlip(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		scurve.extremumSlip = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateSideExtremumValue(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		scurve.extremumValue = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateSideAsymptoteSlip(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		scurve.asymptoteSlip = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateSideAsymptoteValue(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		scurve.asymptoteValue = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateSideStiffness(InputField input ) {
		float val;
		float.TryParse (input.text, out val);

		scurve.stiffness = val;

		OnUpdateWheelFriction ();
	}

	public void OnUpdateWheelFriction() {

		foreach (WheelCollider wc in wheelColliders) {
			wc.forwardFriction = fcurve;
			wc.sidewaysFriction = scurve;
		}

		PlayerPrefs.SetFloat ("forwExtSlip", fcurve.extremumSlip);
		PlayerPrefs.SetFloat ("forwExtValue", fcurve.extremumValue);
		PlayerPrefs.SetFloat ("forwAsymSlip", fcurve.asymptoteSlip);
		PlayerPrefs.SetFloat ("forwAsymValue", fcurve.asymptoteValue);
		PlayerPrefs.SetFloat ("forwStiffness", fcurve.stiffness);

		PlayerPrefs.SetFloat ("sideExtSlip", scurve.extremumSlip);
		PlayerPrefs.SetFloat ("sideExtValue", scurve.extremumValue);
		PlayerPrefs.SetFloat ("sideAsymSlip", scurve.asymptoteSlip);
		PlayerPrefs.SetFloat ("sideAsymValue", scurve.asymptoteValue);
		PlayerPrefs.SetFloat ("sideStiffness", scurve.stiffness);
	}

	public void OnSetToDefaultClick() {
		fcurve.extremumSlip = 0.8f;
		fcurve.extremumValue = 1f;
		fcurve.asymptoteSlip = 0.4f;
		fcurve.asymptoteValue = 0.8f;
		fcurve.stiffness = 2f;

		scurve.extremumSlip = 0.01f;
		scurve.extremumValue = 1f;
		scurve.asymptoteSlip = 0.005f;
		scurve.asymptoteValue = 0.8f;
		scurve.stiffness = 2f;

		forwExtSlip.text = fcurve.extremumSlip.ToString();
		forwExtValue.text = fcurve.extremumValue.ToString();
		forwAsymSlip.text = fcurve.asymptoteSlip.ToString();
		forwAsymValue.text = fcurve.asymptoteValue.ToString();
		forwStiffness.text = fcurve.stiffness.ToString();

		sideExtSlip.text = scurve.extremumSlip.ToString();
		sideExtValue.text = scurve.extremumValue.ToString();
		sideAsymSlip.text = scurve.asymptoteSlip.ToString();
		sideAsymValue.text = scurve.asymptoteValue.ToString();
		sideStiffness.text = scurve.stiffness.ToString();


		OnUpdateWheelFriction ();
	}

	public void StartSimIndex(int index) {

		ResetCar ();

		graphOverlay.carData.ClearAll ();
		foreach (GraphOverlay.WheelConfig wc in graphOverlay.wheelConfigs) {
			wc.ClearAll ();
		}

		SimConfig simConfig = simConfigList [index];

		_currentTurns = simConfig.turns;
		_currentTurnIndex = 0;

		configName = simConfig.configName;

		turnSpeed = simConfig.initSpeed;
		weightConfig = simConfig.weightConfig;

		UpdateWeightConfig ();

		_isSimStarted = true;
		_simConfigIndex = index;

		simState = SimState.ACC;
		_simTimer = 0;
		_turnTimer = 0;
		_restTimer = 0;

	}

	public void OnStartSimClick() {

		StartSimIndex (0);
	}

	public void SaveFile() {


		string path = "Results/";

		if (configName == "") {

			if (weightConfig == WeightConfig.IC) {
				path = "ic";
			} else if (weightConfig == WeightConfig.EV) {
				path = "ev";
			}

			path += "_" + turnSpeed.ToString () + ".csv";

		} else {

			path += configName + ".csv";
		}


		StreamWriter writer = new StreamWriter(path, false);

		writer.Write ("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14} \n", 
			"time(sec)", "speed(km/h)", "roll angle", "FL force", "FR force", "RL force", "RR force",
			"FL long-slip", "FR long-slip", "RL long-slip", "RR long-slip",
			"FL lat-slip", "FR lat-slip", "RL lat-slip", "RR lat-slip" );

		for (int i = 0; i < graphOverlay.carData.rollData.Count; i++) {

			int c1 = graphOverlay.carData.rollData.Count;
			int c2 = graphOverlay.wheelConfigs [0].forceData.Count;
			int c3 = graphOverlay.wheelConfigs [0].longData.Count;
			int c4 = graphOverlay.wheelConfigs [0].latData.Count;

			if (!((c1 == c2) && (c1 == c3) && (c1 == c4))) {
				Debug.LogError ("Something wrong! " + c1 + ", " + c2 + ", " + c3 + ", " + c4);
			}

			float roll = graphOverlay.carData.rollData [i];

			if (roll > 300)
				roll = roll - 360;

			writer.Write ("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14} \n", graphOverlay.carData.timeData[i], graphOverlay.carData.velocityData[i], roll, 
				graphOverlay.wheelConfigs[0].forceData[i], graphOverlay.wheelConfigs[1].forceData[i], graphOverlay.wheelConfigs[2].forceData[i], graphOverlay.wheelConfigs[3].forceData[i],
				graphOverlay.wheelConfigs[0].longData[i], graphOverlay.wheelConfigs[1].longData[i], graphOverlay.wheelConfigs[2].longData[i], graphOverlay.wheelConfigs[3].longData[i],
				graphOverlay.wheelConfigs[0].latData[i], graphOverlay.wheelConfigs[1].latData[i], graphOverlay.wheelConfigs[2].latData[i], graphOverlay.wheelConfigs[3].latData[i]
			);
		}

		writer.Close();

	}

}
