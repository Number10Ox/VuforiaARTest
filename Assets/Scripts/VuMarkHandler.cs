/*===============================================================================
Copyright (c) 2016-2017 PTC Inc. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.
===============================================================================*/
using System.Collections;
using UnityEngine;
using Vuforia;

/// <summary>
/// A custom handler which uses the VuMarkManager.
/// </summary>
public class VuMarkHandler : MonoBehaviour
{
    #region PUBLIC_MEMBER_VARIABLES

    [System.Serializable]
    public class TargetInfo
    {
        public string vumarkId;           
        public GameObject obj;
    }

    public TargetInfo[] targets;

    #endregion // PUBLIC_MEMBER_VARIABLES

    #region PRIVATE_MEMBER_VARIABLES

    // PanelShowHide m_IdPanel;
    VuMarkManager m_VuMarkManager;
    VuMarkTarget m_ClosestVuMark;
    VuMarkTarget m_CurrentVuMark;

    #endregion // PRIVATE_MEMBER_VARIABLES


    #region UNITY_MONOBEHAVIOUR_METHODS

    void Start()
    {
        // register callbacks to VuMark Manager
        m_VuMarkManager = TrackerManager.Instance.GetStateManager().GetVuMarkManager();
        m_VuMarkManager.RegisterVuMarkDetectedCallback(OnVuMarkDetected);
        m_VuMarkManager.RegisterVuMarkLostCallback(OnVuMarkLost);

        HideAllGameObjects();
    }

    void Update()
    {
        UpdateActiveObjects();
    }

    void OnDestroy()
    {
        // unregister callbacks from VuMark Manager
        m_VuMarkManager.UnregisterVuMarkDetectedCallback(OnVuMarkDetected);
        m_VuMarkManager.UnregisterVuMarkLostCallback(OnVuMarkLost);
    }

    #endregion // UNITY_MONOBEHAVIOUR_METHODS

    #region PUBLIC_METHODS

    /// <summary>
    /// This method will be called whenever a new VuMark is detected
    /// </summary>
    public void OnVuMarkDetected(VuMarkTarget target)
    {
        Debug.Log("New VuMark: " + GetVuMarkId(target));
    }

    /// <summary>
    /// This method will be called whenever a tracked VuMark is lost
    /// </summary>
    public void OnVuMarkLost(VuMarkTarget target)
    {
        Debug.Log("Lost VuMark: " + GetVuMarkId(target));

        string vuMarkId = GetVuMarkId(target);
        GameObject associatedObject = FindObjectForVuMark(vuMarkId);
        if (associatedObject != null)
        {
            associatedObject.SetActive(false);
        }
    }

    #endregion // PUBLIC_METHODS

    #region PRIVATE_METHODS

    void UpdateActiveObjects()
    {
        foreach (var bhvr in m_VuMarkManager.GetActiveBehaviours())
        {
            string vuMarkId = GetVuMarkId(bhvr.VuMarkTarget);
            GameObject associatedObject = FindObjectForVuMark(vuMarkId);
            if (associatedObject != null)
            {
                associatedObject.SetActive(true);
                Vector3 vuMarkPosition = bhvr.transform.position;
                Quaternion vuMarkRotation = bhvr.transform.rotation;
                Quaternion zInverted = Quaternion.AngleAxis(-180.0f, Vector3.up);
                //vuMarkPosition.z += .25f;
                associatedObject.transform.position = vuMarkPosition;
                associatedObject.transform.rotation = bhvr.transform.rotation;// * zInverted;
            }
            else
            {
                Debug.Log("ERROR: Can't find object for VuMark: " + vuMarkId);
            }
        }
    }

    void UpdateClosestTarget()
    {
        Camera cam = DigitalEyewearARController.Instance.PrimaryCamera ?? Camera.main;

        float closestDistance = Mathf.Infinity;

        foreach (var bhvr in m_VuMarkManager.GetActiveBehaviours())
        {
            Vector3 worldPosition = bhvr.transform.position;
            Vector3 camPosition = cam.transform.InverseTransformPoint(worldPosition);

            float distance = Vector3.Distance(Vector2.zero, camPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                m_ClosestVuMark = bhvr.VuMarkTarget;
            }
        }

        if (m_ClosestVuMark != null &&
            m_CurrentVuMark != m_ClosestVuMark)
        {
            var vuMarkId = GetVuMarkId(m_ClosestVuMark);
            var vuMarkDataType = GetVuMarkDataType(m_ClosestVuMark);
            var vuMarkImage = GetVuMarkImage(m_ClosestVuMark);
            var vuMarkDesc = GetNumericVuMarkDescription(m_ClosestVuMark);

            m_CurrentVuMark = m_ClosestVuMark;

            StartCoroutine(ShowPanelAfter(0.5f, vuMarkId, vuMarkDataType, vuMarkDesc, vuMarkImage));
        }
    }

    IEnumerator ShowPanelAfter(float seconds, string vuMarkId, string vuMarkDataType, string vuMarkDesc, Sprite vuMarkImage)
    {
        yield return new WaitForSeconds(seconds);

        //m_IdPanel.Show(vuMarkId, vuMarkDataType, vuMarkDesc, vuMarkImage);
    }

    string GetVuMarkDataType(VuMarkTarget vumark)
    {
        switch (vumark.InstanceId.DataType)
        {
            case InstanceIdType.BYTES:
                return "Bytes";
            case InstanceIdType.STRING:
                return "String";
            case InstanceIdType.NUMERIC:
                return "Numeric";
        }
        return string.Empty;
    }

    string GetVuMarkId(VuMarkTarget vumark)
    {
        switch (vumark.InstanceId.DataType)
        {
            case InstanceIdType.BYTES:
                return vumark.InstanceId.HexStringValue;
            case InstanceIdType.STRING:
                return vumark.InstanceId.StringValue;
            case InstanceIdType.NUMERIC:
                return vumark.InstanceId.NumericValue.ToString();
        }
        return string.Empty;
    }

    Sprite GetVuMarkImage(VuMarkTarget vumark)
    {
        var instanceImg = vumark.InstanceImage;
        if (instanceImg == null)
        {
            Debug.Log("VuMark Instance Image is null.");
            return null;
        }

        // First we create a texture
        Texture2D texture = new Texture2D(instanceImg.Width, instanceImg.Height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp
        };
        instanceImg.CopyToTexture(texture);

        // Then we turn the texture into a Sprite
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
    }

    string GetNumericVuMarkDescription(VuMarkTarget vumark)
    {
        int vuMarkIdNumeric;

        if (int.TryParse(GetVuMarkId(vumark), out vuMarkIdNumeric))
        {
            // Change the description based on the VuMark id
            switch (vuMarkIdNumeric % 4)
            {
                case 1:
                    return "Astronaut";
                case 2:
                    return "Drone";
                case 3:
                    return "Fissure";
                case 0:
                    return "Oxygen Tank";
                default:
                    return "Astronaut";
            }
        }

        return string.Empty; // if VuMark DataType is byte or string
    }

    GameObject FindObjectForVuMark(string vuMarkId)
    {
        // TODO Could create associative array
        for (int i = 0; i < targets.Length; i++)
        {
            string targetVuMarkId = targets[i].vumarkId;
            if (targetVuMarkId == vuMarkId)
            {
                return targets[i].obj;
            }
        }

        return null;
    }

    void HideAllGameObjects()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject obj = targets[i].obj;
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    #endregion // PRIVATE_METHODS
}
