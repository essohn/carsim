using UnityEngine;
using System.Collections.Generic;

public class CarSwitcher : MonoBehaviour
{
	public List<GameObject> vehicles;
	public Transform spawnPoints;

	private DriftCamera m_DriftCamera;
	private int m_VehicleId;

	private int weightIndex;

	void Start () 
    {
		m_DriftCamera = GetComponent<DriftCamera>();
	}
	
	void Update () 
    {
		if (Input.GetKeyUp (KeyCode.W)) {
			weightIndex++;

			if (weightIndex >= 3) {
				weightIndex = 0;
			}

			Transform vehicleT = vehicles[m_VehicleId].transform;

			Transform batteryPack = vehicleT.Find ("Battery Pack");
			Transform engineBlock = vehicleT.Find ("Engine Block");

			if (weightIndex == 0) {
				batteryPack.gameObject.SetActive (false);
				engineBlock.gameObject.SetActive (false);
			} else if (weightIndex == 1) {
				batteryPack.gameObject.SetActive (true);
				engineBlock.gameObject.SetActive (false);
			} else if (weightIndex == 2) {
				batteryPack.gameObject.SetActive (false);
				engineBlock.gameObject.SetActive (true);
			}

		}




		if (Input.GetKeyUp(KeyCode.K))	
		{
			// Disable the previous vehicle.
			vehicles[m_VehicleId].SetActive(false);

			m_VehicleId = (m_VehicleId + 1) % vehicles.Count;

			vehicles[m_VehicleId].SetActive(true);

			var graph = GetComponent<GraphOverlay>();
			if (graph)
			{
				graph.vehicleBody = vehicles[m_VehicleId].GetComponent<Rigidbody>();
				graph.SetupWheelConfigs();
			}

			// Setup the new one.
			Transform vehicleT = vehicles[m_VehicleId].transform;
			Transform camRig = vehicleT.Find("CamRig");

			m_DriftCamera.lookAtTarget = camRig.Find("CamLookAtTarget");
			m_DriftCamera.positionTarget = camRig.Find("CamPosition");
			m_DriftCamera.sideView = camRig.Find("CamSidePosition");
			m_DriftCamera.behindView = camRig.Find("CamBehindPosition");


		}

		if (Input.GetKeyUp(KeyCode.R))
		{
			Transform vehicleTransform = vehicles[m_VehicleId].transform;
			vehicleTransform.rotation = Quaternion.identity;

			Transform closest = spawnPoints.GetChild(0);

			// Find the closest spawn point.
			for (int i = 0; i < spawnPoints.childCount; ++i)
			{
				Transform thisTransform = spawnPoints.GetChild(i);

				float distanceToClosest = Vector3.Distance(closest.position, vehicleTransform.position);
				float distanceToThis = Vector3.Distance(thisTransform.position, vehicleTransform.position);

				if (distanceToThis < distanceToClosest)
				{
					closest = thisTransform;
				}
			}

			// Spawn at the closest spawn point.
#if UNITY_EDITOR
			Debug.Log("Teleporting to " + closest.name);
#endif
			vehicleTransform.rotation = closest.rotation;

			// Try refining the spawn point so it's closer to the ground.
            // Here we assume there is only one renderer.  If not, looping over all the bounds could do the trick.
			var renderer = vehicleTransform.gameObject.GetComponentInChildren<MeshRenderer>();
            // A valid car must have at least one wheel.
			var wheel = vehicleTransform.gameObject.GetComponentInChildren<WheelCollider>(); 

			RaycastHit hit;
            // Boxcast everything except cars.
			if (Physics.BoxCast(closest.position, renderer.bounds.extents, Vector3.down, out hit, vehicleTransform.rotation, float.MaxValue, ~(1 << LayerMask.NameToLayer("Car"))) )
			{
				vehicleTransform.position = closest.position + Vector3.down * (hit.distance - wheel.radius);
			}
			else
			{
				Debug.Log("Failed to locate the ground below the spawn point " + closest.name);
				vehicleTransform.position = closest.position;
			}

			// Reset the velocity.
			var vehicleBody = vehicleTransform.gameObject.GetComponent<Rigidbody>();
			vehicleBody.velocity = Vector3.zero;
			vehicleBody.angularVelocity = Vector3.zero;
		}
	}
}
