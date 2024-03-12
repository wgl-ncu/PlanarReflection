using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlanarReflectionManager : MonoBehaviour
{
    public LayerMask layerMask;
    public Camera reflectionCam;
    public bool useReflectionMatrix = false;
    public float planeOffset = 0.01f;
    private List<Transform> planes = new List<Transform>();
    private RenderTexture _reflectionTexture;
    private int _PlanaReflectionTextureId = Shader.PropertyToID("_PlanaReflectionTexture");
    private float PlaneOffset => useReflectionMatrix ? planeOffset : 0f;

    private static PlanarReflectionManager _instance;
    public static PlanarReflectionManager Instance
    {
        get
        {
            if(null == _instance)
            {
                _instance = GameObject.Find("PlanarReflectionManager").GetComponent<PlanarReflectionManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        reflectionCam = CreateReflectCamera();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    public void Register(Transform trans)
    {
        planes.Add(trans);
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType == CameraType.Game)
        {
            CreatePlanarReflectionTexture(camera);
            RefreshReflectionCam(camera);
            foreach (var plane in planes)
            {
                RenderPlanarReflection(context, camera, plane);
                Shader.SetGlobalTexture(_PlanaReflectionTextureId, _reflectionTexture);
            }
        }
    }

    private void RefreshReflectionCam(Camera src)
    {
        reflectionCam.aspect = src.aspect;
        reflectionCam.cameraType = src.cameraType;
        reflectionCam.clearFlags = src.clearFlags;
        reflectionCam.fieldOfView = src.fieldOfView;
        reflectionCam.depth = src.depth;
        reflectionCam.farClipPlane = src.farClipPlane;
        reflectionCam.focalLength = src.focalLength;
        reflectionCam.useOcclusionCulling = false;
        if (reflectionCam.gameObject.TryGetComponent(out UniversalAdditionalCameraData camData))
        {
            camData.renderShadows = false;
        }
    }

    private void RenderPlanarReflection(ScriptableRenderContext context, Camera camera, Transform plane)
    {
        if (!useReflectionMatrix)
        {
            SetWithChangeRefectionCam(context, camera, plane);
        }
        else
        {
            SetWithReflectionMatrix(context, camera, plane);
        }

        Vector3 planeNormal = plane.transform.up;
        Vector3 planePos = plane.transform.position + planeNormal * PlaneOffset;
        var clipPlane = CameraSpacePlane(reflectionCam, planePos, planeNormal, 1.0f);
        reflectionCam.projectionMatrix = CalculateObliqueMatrix(camera, clipPlane);

        reflectionCam.cullingMask = layerMask;

        UniversalRenderPipeline.RenderSingleCamera(context, reflectionCam);
    }

    private void SetWithChangeRefectionCam(ScriptableRenderContext context, Camera camera, Transform plane)
    {
        Vector3 camPosWS = camera.transform.position;
        var planeWorld2Local = plane.worldToLocalMatrix;
        var planeLoacl2World = plane.localToWorldMatrix;
        Vector3 camPosOS = planeWorld2Local.MultiplyPoint(camPosWS);
        Vector3 camReflectionPosOS = Vector3.Scale(camPosOS, new Vector3(1, -1, 1));
        Vector3 camReflectionPosWS = planeLoacl2World.MultiplyVector(camReflectionPosOS);
        reflectionCam.transform.position = camReflectionPosWS;
        
        Vector3 camForwardWS = camera.transform.forward;
        Vector3 camForwardOS = planeWorld2Local.MultiplyVector(camForwardWS);
        Vector3 camUpWS = camera.transform.up;
        Vector3 camUpOS = planeWorld2Local.MultiplyVector(camUpWS);
        Vector3 camReflectionForwardOS = Vector3.Scale(camForwardOS, new Vector3(1, -1, 1));
        Vector3 camReflectionUpOS = Vector3.Scale(camUpOS, new Vector3(-1, 1, -1));
        Vector3 camReflectionForwardWS = planeLoacl2World.MultiplyVector(camReflectionForwardOS);
        Vector3 camReflectionUpWS = planeLoacl2World.MultiplyVector(camReflectionUpOS);
        reflectionCam.transform.rotation = Quaternion.LookRotation(camReflectionForwardWS, camReflectionUpWS);
    }

    private void SetWithReflectionMatrix(ScriptableRenderContext context, Camera camera, Transform plane)
    {
        Vector3 camPosWS = camera.transform.position;
        var planeWorld2Local = plane.worldToLocalMatrix;
        var planeLoacl2World = plane.localToWorldMatrix;
        Vector3 camPosOS = planeWorld2Local.MultiplyPoint(camPosWS);
        Vector3 camReflectionPosOS = Vector3.Scale(camPosOS, new Vector3(1, -1, 1));
        Vector3 camReflectionPosWS = planeLoacl2World.MultiplyVector(camReflectionPosOS);
        reflectionCam.transform.position = camReflectionPosWS;
        
        Vector3 planePos = plane.position;
        Vector3 planeNormal = plane.up;
        var planeV4 = GetPlane(planePos, planeNormal);
        var reflectionMatrix = CreateReflectionMatrix(planeV4);
        reflectionCam.worldToCameraMatrix = camera.worldToCameraMatrix *  reflectionMatrix;
    }

    private Matrix4x4 CreateReflectionMatrix(Vector4 plane)
    {
        var x = plane[0];
        var y = plane[1];
        var z = plane[2];
        var d = plane[3];

        Vector4 row1 = new Vector4(1f - 2f * x * x, -2f * x * y, -2f * x * z, -2f * x * d);
        Vector4 row2 = new Vector4(-2f * x * y, 1f - 2f * y * y, -2f * y * z, -2f * y * d);
        Vector4 row3 = new Vector4(-2f * x * z, -2f * y * z, 1f - 2f * z * z, -2f * z * d);
        Vector4 row4 = new Vector4(0, 0, 0, 1);

        Matrix4x4 reflectionMatrix = new Matrix4x4();
        reflectionMatrix.SetRow(0, row1);
        reflectionMatrix.SetRow(1, row2);
        reflectionMatrix.SetRow(2, row3);
        reflectionMatrix.SetRow(3, row4);

        return reflectionMatrix;
    }

    private Camera CreateReflectCamera()
    {
        var go = reflectionCam.gameObject;
        var cameraData = go.GetComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;

        //go.transform.parent = transform;

        cameraData.requiresColorOption = CameraOverrideOption.Off;
        cameraData.requiresDepthOption = CameraOverrideOption.Off;
        cameraData.renderShadows = false;
        cameraData.SetRenderer(0);

        var t = transform;
        var reflectionCamera = go.GetComponent<Camera>();
        reflectionCamera.transform.SetPositionAndRotation(transform.position, t.rotation);
        reflectionCamera.depth = -10;
        reflectionCamera.enabled = false;
        go.hideFlags = HideFlags.HideAndDontSave;

        return reflectionCamera;
    }

    private void CreatePlanarReflectionTexture(Camera cam)
    {
        if (_reflectionTexture == null)
        {
            var res = ReflectionResolution(cam, UniversalRenderPipeline.asset.renderScale);
            const bool useHdr10 = true;
            const RenderTextureFormat hdrFormat = useHdr10 ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.DefaultHDR;
            _reflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 16,
                GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
        }
        reflectionCam.targetTexture = _reflectionTexture;
    }

    private int2 ReflectionResolution(Camera cam, float scale)
    {
        var x = (int)(cam.pixelWidth * scale * 0.5f);
        var y = (int)(cam.pixelHeight * scale * 0.5f);
        return new int2(x, y);
    }
    
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
        var offsetPos = pos + normal * PlaneOffset;
        var m = cam.worldToCameraMatrix;
        var cameraPosition = m.MultiplyPoint(offsetPos);
        var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
        return GetPlane(cameraPosition, cameraNormal);
    }

    private Vector4 GetPlane(Vector3 pos, Vector3 normal)
    {
        return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(pos, normal));
    }
    
    private Matrix4x4 CalculateObliqueMatrix(Camera cam, Vector4 plane) {
         var new_M = cam.CalculateObliqueMatrix(plane);
        return new_M;
    }
}
