using System;
using UnityEngine;

public class DriftCamera : MonoBehaviour
{
    [Serializable]
    public class AdvancedOptions
    {
        public bool updateCameraInUpdate;
        public bool updateCameraInFixedUpdate = true;
        public bool updateCameraInLateUpdate;
        public KeyCode switchViewKey = KeyCode.Space;
    }

    public float smoothing = 6f;
    public Transform lookAtTarget;
    public Transform positionTarget;
	public Transform sideView;
	public Transform behindView;
    public AdvancedOptions advancedOptions;

    //bool m_ShowingSideView;
	int showingIndex;

    private void FixedUpdate ()
    {
        if(advancedOptions.updateCameraInFixedUpdate)
            UpdateCamera ();
    }

    private void Update ()
    {
		if (Input.GetKeyDown (advancedOptions.switchViewKey))
			showingIndex++;


		if (showingIndex >= 3)
			showingIndex = 0;

        if(advancedOptions.updateCameraInUpdate)
            UpdateCamera ();
    }

    private void LateUpdate ()
    {
        if(advancedOptions.updateCameraInLateUpdate)
            UpdateCamera ();
    }

    private void UpdateCamera ()
    {
		if (showingIndex == 0) {
			transform.position = sideView.position;
			transform.rotation = sideView.rotation;
		} else if (showingIndex == 1) {
			transform.position = Vector3.Lerp (transform.position, positionTarget.position, Time.deltaTime * smoothing);

			Vector3 pos = transform.position;
			if (pos.y < 0)
				pos.y = - pos.y;
			transform.position = pos;

			transform.LookAt (lookAtTarget);
		} else if (showingIndex == 2) {
			transform.position = behindView.position;

			Vector3 pos = transform.position;
			if (pos.y < 0)
				pos.y = - pos.y;
			transform.position = pos;

			transform.LookAt (lookAtTarget);
		}
    }
}
