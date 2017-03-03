﻿//////////////////////////////////////////////////////////////////////////
//
//  Copyright (C) 2007-2016 zSpace, Inc.  All Rights Reserved.
//
//////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace zSpace.Core.Samples
{
    public class StylusObjectManipulationSample : MonoBehaviour
    {
        //////////////////////////////////////////////////////////////////
        // Unity Monobehaviour Callbacks
        //////////////////////////////////////////////////////////////////

        private bool cut = false;

        void Start()
        {
            _core = GameObject.FindObjectOfType<ZCore>();
            MeshFilter[] tab_mesh = GameObject.FindObjectsOfType<MeshFilter>();
           
            Debug.Log(tab_mesh.Length);
            if (_core == null)
            {
                Debug.LogError("Unable to find reference to zSpace.Core.Core Monobehaviour.");
                this.enabled = false;
                return;
            }

            // Create the stylus beam's GameObject.
            _stylusBeamObject = new GameObject("StylusBeam");
            _stylusBeamRenderer = _stylusBeamObject.AddComponent<LineRenderer>();
            _stylusBeamRenderer.material = new Material(Shader.Find("Transparent/Diffuse"));
            _stylusBeamRenderer.SetColors(Color.white, Color.white);
        }

        void Update()
        {
            // Grab the latest stylus pose and button state information.
            ZCore.Pose pose = _core.GetTargetPose(ZCore.TargetType.Primary, ZCore.CoordinateSpace.World);
            bool isButtonPressed = _core.IsTargetButtonPressed(ZCore.TargetType.Primary, 0);
            bool isButtonPressed1 = _core.IsTargetButtonPressed(ZCore.TargetType.Primary, 1);
            bool isButtonPressed2 = _core.IsTargetButtonPressed(ZCore.TargetType.Primary, 2);
            switch (_stylusState)
            {
                case StylusState.Idle:
                    {
                        _stylusBeamLength = DEFAULT_STYLUS_BEAM_LENGTH;

                        // Perform a raycast on the entire scene to determine what the
                        // stylus is currently colliding with.
                        RaycastHit hit;
                        if (Physics.Raycast(pose.Position, pose.Direction, out hit))
                        {
                            // Update the stylus beam length.
                            _stylusBeamLength = hit.distance / _core.ViewerScale;
                            // If the front stylus button was pressed, initiate a grab.
                            if (isButtonPressed1 && !_wasButtonPressed)
                            {
                                this.BeginGrabCut(hit.collider.gameObject);
                                cut = true;
                            }
                            if (isButtonPressed2 && !_wasButtonPressed)
                            {
                                cut = false;
                                _grabObjectList = new List<GameObject>();
                            }
                            if (isButtonPressed && !_wasButtonPressed)
                            {

                                // Begin the grab.
                                //this.BeginGrab(hit.collider.gameObject , hit.distance, pose.Position, pose.Rotation);
                                this.BeginGrabMultiple(hit.collider.gameObject.name, hit.distance, pose.Position, pose.Rotation, cut);


                                _stylusState = StylusState.Grab;
                            }
                        }
                    }
                    break;

                case StylusState.Grab:
                    {
                        // Update the grab.
                        this.UpdateGrabMultiple(pose.Position, pose.Rotation);

                        // End the grab if the front stylus button was released.
                        if (!isButtonPressed && _wasButtonPressed)
                        {
                            _stylusState = StylusState.Idle;
                            _grabObjectList = new List<GameObject>();
                        }
                    }
                    break;

                default:
                    break;
            }

            // Update the stylus beam.
            this.UpdateStylusBeam(pose.Position, pose.Direction);

            // Cache state for next frame.
            _wasButtonPressed = isButtonPressed;
        }


        //////////////////////////////////////////////////////////////////
        // Private Helpers
        //////////////////////////////////////////////////////////////////  
        
        private void BeginGrabCut(GameObject hitObject)
        {
            int a = 0;
            if (_grabObjectList.Count == 0)
            {
                _grabObjectList.Add(hitObject);
                Debug.Log("added");
            }
            foreach (GameObject GO in _grabObjectList)
            //if (hitObject.transform.position != hitObject.transform.position || hitObject.transform.rotation != hitObject.transform.rotation)
                if(GO.GetInstanceID() == hitObject.GetInstanceID())
                {
                    a = -1;
                    break;
                }
            if(a == 0)
            {
                _grabObjectList.Add(hitObject);
                Debug.Log("added");
            }

        }

        private void BeginGrabMultiple(string id, float hitDistance, Vector3 inputPosition, Quaternion inputRotation, bool b)
        {
            List<GameObject> hitObject = new List<GameObject>();
            Vector3 inputEndPosition = inputPosition + (inputRotation * (Vector3.forward * hitDistance));

            _initialGrabOffsetList = new List<Vector3>();
            _initialGrabRotationList = new List<Quaternion>();
            var tab = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == id);

            if(b)
            {
                tab = _grabObjectList;
            }
            else
            {
                _grabObjectList = new List<GameObject>();

            }

            foreach (var t in tab)
                {
                    hitObject.Add(t);
                }

                // Cache the initial grab state.
                _grabObjectList = hitObject;
            Debug.Log("size : " + _grabObjectList.Count);
            foreach (GameObject GO in hitObject)
            {
                _initialGrabOffsetList.Add(new Vector3());
                _initialGrabOffsetList[_initialGrabOffsetList.Count - 1] = Quaternion.Inverse(GO.transform.localRotation) * (GO.transform.localPosition - inputEndPosition);
                _initialGrabRotationList.Add(new Quaternion());
                _initialGrabRotationList[_initialGrabRotationList.Count -1] = Quaternion.Inverse(inputRotation) * GO.transform.localRotation;
            }
            _initialGrabDistanceList = hitDistance;

        }

        private void UpdateGrabMultiple(Vector3 inputPosition, Quaternion inputRotation)
        {
            Vector3 inputEndPosition = inputPosition + (inputRotation * (Vector3.forward * _initialGrabDistanceList));

            // Update the grab object's rotation.
            for (int i = 0; i< _grabObjectList.Count;i++)
            {
                Quaternion objectRotation = inputRotation * _initialGrabRotationList[i];
                _grabObjectList[i].transform.rotation = objectRotation;

                // Update the grab object's position.
                Vector3 objectPosition = inputEndPosition + (objectRotation * _initialGrabOffsetList[i] );
                _grabObjectList[i].transform.position = objectPosition;
            }
        }

        private void BeginGrab(GameObject hitObject, float hitDistance, Vector3 inputPosition, Quaternion inputRotation)
        {
            Vector3 inputEndPosition = inputPosition + (inputRotation * (Vector3.forward * hitDistance));

            // Cache the initial grab state.
            _grabObject          = hitObject;
            _initialGrabOffset   = Quaternion.Inverse(hitObject.transform.localRotation) * (hitObject.transform.localPosition - inputEndPosition);
            _initialGrabRotation = Quaternion.Inverse(inputRotation) * hitObject.transform.localRotation;
            _initialGrabDistance = hitDistance;
        }

        private void UpdateGrab(Vector3 inputPosition, Quaternion inputRotation)
        {
            Vector3 inputEndPosition = inputPosition + (inputRotation * (Vector3.forward * _initialGrabDistance));

            // Update the grab object's rotation.
            Quaternion objectRotation = inputRotation * _initialGrabRotation;
            _grabObject.transform.rotation = objectRotation;

            // Update the grab object's position.
            Vector3 objectPosition = inputEndPosition + (objectRotation * _initialGrabOffset);
            _grabObject.transform.position = objectPosition;
        }

        private void UpdateStylusBeam(Vector3 stylusPosition, Vector3 stylusDirection)
        {
            if (_stylusBeamRenderer != null)
            {
                float stylusBeamWidth  = DEFAULT_STYLUS_BEAM_WIDTH * _core.ViewerScale;
                float stylusBeamLength = _stylusBeamLength * _core.ViewerScale;

                _stylusBeamRenderer.SetWidth(stylusBeamWidth, stylusBeamWidth);
                _stylusBeamRenderer.SetPosition(0, stylusPosition);
                _stylusBeamRenderer.SetPosition(1, stylusPosition + (stylusDirection * stylusBeamLength));
            }
        }


        //////////////////////////////////////////////////////////////////
        // Private Enumerations
        //////////////////////////////////////////////////////////////////

        private enum StylusState
        {
            Idle = 0,
            Grab = 1,
        }


        //////////////////////////////////////////////////////////////////
        // Private Members
        //////////////////////////////////////////////////////////////////

        private static readonly float DEFAULT_STYLUS_BEAM_WIDTH  = 0.0002f;
        private static readonly float DEFAULT_STYLUS_BEAM_LENGTH = 0.3f;

        private ZCore         _core = null;
        private bool         _wasButtonPressed = false;

        private GameObject   _stylusBeamObject   = null;
        private LineRenderer _stylusBeamRenderer = null;
        private float        _stylusBeamLength   = DEFAULT_STYLUS_BEAM_LENGTH;

        private StylusState  _stylusState         = StylusState.Idle;

        private GameObject   _grabObject          = null;
        private Vector3      _initialGrabOffset   = Vector3.zero;
        private Quaternion   _initialGrabRotation = Quaternion.identity;
        private float        _initialGrabDistance = 0.0f;

        private List<GameObject> _grabObjectList = new List<GameObject>();
        private List<Vector3> _initialGrabOffsetList = new List<Vector3>();
        private List<Quaternion> _initialGrabRotationList = new List<Quaternion>();
        private float _initialGrabDistanceList = 0.0f;
    }
}