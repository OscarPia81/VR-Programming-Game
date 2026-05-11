using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ConnectionManager : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("Visuals")]
    public Material previewLineMaterial;
    public Material connectionLineMaterial;

    [Header("Settings")]
    public LayerMask blockLayerMask = -1;
    public float maxPreviewDistance = 20f;

    private InputAction rightActivateAction;
    private NearFarInteractor rightNearFarInteractor;
    private Transform fallbackRayOrigin;
    private CodeManager codeManager;
    private Code selectedBlock;

    private GameObject previewContainer;
    private LineRenderer previewLineRenderer;
    private GameObject previewArrowhead;

    private readonly List<ConnectionData> connections = new List<ConnectionData>();
    private GameObject connectionsContainer;

    private class ConnectionData
    {
        public Code from;
        public Code to;
        public LineRenderer line;
        public GameObject arrowhead;
    }

    private void Start()
    {
        codeManager = FindObjectOfType<CodeManager>();

        if (inputActions == null)
        {
            Debug.LogError("[CM] inputActions is null");
            return;
        }

        var rightMap = inputActions.FindActionMap("XRI Right Interaction");
        if (rightMap == null)
        {
            Debug.LogError("[CM] Action map 'XRI Right Interaction' not found");
            return;
        }

        rightActivateAction = rightMap.FindAction("Activate");
        if (rightActivateAction == null)
        {
            Debug.LogError("[CM] 'Activate' not found in XRI Right Interaction");
            return;
        }

        rightActivateAction.Enable();
        rightActivateAction.performed += OnActivatePerformed;
        Debug.Log("[CM] Listening to right Activate");

        FindRayOrigin();

        if (previewLineMaterial == null)
            previewLineMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { color = new Color(0.3f, 0.7f, 1f, 0.5f) };
        if (connectionLineMaterial == null)
            connectionLineMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { color = new Color(1f, 0.85f, 0f, 0.6f) };

        connectionsContainer = new GameObject("ConnectionLines");
        connectionsContainer.transform.SetParent(transform);
    }

    private void OnDestroy()
    {
        if (rightActivateAction != null)
        {
            rightActivateAction.performed -= OnActivatePerformed;
            rightActivateAction.Disable();
        }
    }

    private void FindRayOrigin()
    {
        var xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
        {
            var rightCtrl = FindDeepChild(xrOrigin.transform, "Right Controller");
            if (rightCtrl != null)
            {
                var nfi = rightCtrl.GetComponentInChildren<NearFarInteractor>(includeInactive: true);
                if (nfi != null)
                {
                    rightNearFarInteractor = nfi;
                    Debug.Log($"[CM] Ray origin: NearFarInteractor on '{nfi.gameObject.name}'");
                    return;
                }

                fallbackRayOrigin = rightCtrl;
                Debug.Log("[CM] Ray origin: Right Controller Transform");
                return;
            }
        }

        var cam = Camera.main;
        if (cam != null)
        {
            fallbackRayOrigin = cam.transform;
            Debug.Log("[CM] Ray origin: Camera.main (fallback)");
        }
        else
        {
            Debug.LogError("[CM] No ray origin found");
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void Update()
    {
        UpdatePreviewLine();
        UpdateAllConnectionLines();
    }

    private void OnActivatePerformed(InputAction.CallbackContext ctx)
    {
        HandleActivatePress();
    }

    private bool TryGetRay(out Ray ray)
    {
        if (rightNearFarInteractor != null && rightNearFarInteractor.curveOrigin != null)
        {
            var origin = rightNearFarInteractor.curveOrigin;
            ray = new Ray(origin.position, origin.forward);
            return true;
        }

        if (fallbackRayOrigin != null)
        {
            ray = new Ray(fallbackRayOrigin.position, fallbackRayOrigin.forward);
            return true;
        }

        ray = default;
        return false;
    }

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private void HandleActivatePress()
    {
        if (codeManager != null && codeManager.IsExecuting)
        {
            Debug.Log("[CM] Blocked: code executing");
            return;
        }

        if (!TryGetRay(out Ray ray))
            return;

        int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, maxPreviewDistance, blockLayerMask);
        Code hitBlock = null;

        for (int i = 0; i < hitCount; i++)
        {
            var block = hitBuffer[i].collider.GetComponentInParent<Code>();
            if (block != null)
            {
                hitBlock = block;
                break;
            }
        }

        if (hitBlock == null)
        {
            Debug.Log($"[CM] Hit nothing (ray hit {hitCount} objects, no Code)");
        }
        else if (!IsConnectable(hitBlock))
        {
            Debug.Log($"[CM] Hit non-MoveCode: '{hitBlock.name}' ({hitBlock.GetType().Name})");
            hitBlock = null;
        }

        if (selectedBlock == null)
        {
            if (hitBlock != null)
            {
                Debug.Log($"[CM] Select '{hitBlock.name}'");
                SelectBlock(hitBlock);
            }
        }
        else
        {
            if (hitBlock != null && hitBlock != selectedBlock)
            {
                if (TryConnect(selectedBlock, hitBlock))
                {
                    Debug.Log($"[CM] Connect '{selectedBlock.name}' -> '{hitBlock.name}'");
                    Connect(selectedBlock, hitBlock);
                }
                else
                {
                    Debug.Log($"[CM] Rejected '{selectedBlock.name}' -> '{hitBlock.name}'");
                }
                DeselectBlock();
            }
            else if (hitBlock == selectedBlock)
            {
                Debug.Log($"[CM] Disconnect self '{selectedBlock.name}'");
                Disconnect(selectedBlock);
                DeselectBlock();
            }
            else
            {
                Debug.Log($"[CM] Disconnect '{selectedBlock.name}' (no target)");
                Disconnect(selectedBlock);
                DeselectBlock();
            }
        }
    }

    private bool IsConnectable(Code block)
    {
        return block is MoveCode;
    }

    private void SelectBlock(Code block)
    {
        selectedBlock = block;
        block.SetHighlight(true);

        if (previewContainer == null)
        {
            CreatePreviewObjects();
        }
        previewContainer.SetActive(true);
    }

    private void DeselectBlock()
    {
        if (selectedBlock != null)
        {
            selectedBlock.SetHighlight(false);
            selectedBlock = null;
        }

        if (previewContainer != null)
        {
            previewContainer.SetActive(false);
        }
    }

    private bool TryConnect(Code from, Code to)
    {
        if (from == to) return false;
        if (WouldCreateCycle(from, to)) return false;
        if (IsAnyonesNext(to, exceptFrom: from)) return false;
        return true;
    }

    private bool WouldCreateCycle(Code from, Code to)
    {
        Code current = to;
        int maxIterations = 1000;
        int iterations = 0;

        while (current != null && iterations < maxIterations)
        {
            if (current == from) return true;
            current = current.next;
            iterations++;
        }

        return false;
    }

    private bool IsAnyonesNext(Code target, Code exceptFrom)
    {
        MoveCode[] allBlocks = FindObjectsOfType<MoveCode>();
        foreach (MoveCode block in allBlocks)
        {
            if (block.next == target && block != exceptFrom)
            {
                return true;
            }
        }
        return false;
    }

    private void Connect(Code from, Code to)
    {
        Disconnect(from);

        from.next = to;

        Rigidbody rb = to.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        CreateConnectionLine(from, to);
    }

    private void Disconnect(Code from)
    {
        Code oldNext = from.next;
        if (oldNext != null)
        {
            RemoveConnectionLine(from, oldNext);

            if (!IsAnyonesNext(oldNext, exceptFrom: from))
            {
                Rigidbody rb = oldNext.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        }

        from.next = null;
    }

    private void CreateConnectionLine(Code from, Code to)
    {
        GameObject lineObj = new GameObject($"Connection_{from.name}_to_{to.name}");
        lineObj.transform.SetParent(connectionsContainer.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = connectionLineMaterial;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.SetPosition(0, from.GetOutputPoint());
        lr.SetPosition(1, to.GetInputPoint());

        GameObject arrowhead = ArrowheadGenerator.CreateArrowhead(lineObj.transform, connectionLineMaterial);

        Debug.Log($"[CM] Created line '{from.name}'->'{to.name}' start={from.GetOutputPoint()} end={to.GetInputPoint()} material={connectionLineMaterial?.name ?? "null"}");

        connections.Add(new ConnectionData
        {
            from = from,
            to = to,
            line = lr,
            arrowhead = arrowhead
        });
    }

    private void RemoveConnectionLine(Code from, Code to)
    {
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            if (connections[i].from == from && connections[i].to == to)
            {
                if (connections[i].line != null)
                {
                    Destroy(connections[i].line.gameObject);
                }
                connections.RemoveAt(i);
                return;
            }
        }
    }

    private void CreatePreviewObjects()
    {
        previewContainer = new GameObject("PreviewLine");
        previewContainer.transform.SetParent(transform);

        previewLineRenderer = previewContainer.AddComponent<LineRenderer>();
        previewLineRenderer.material = previewLineMaterial;
        previewLineRenderer.startWidth = 0.05f;
        previewLineRenderer.endWidth = 0.05f;
        previewLineRenderer.positionCount = 2;
        previewLineRenderer.useWorldSpace = true;

        previewArrowhead = ArrowheadGenerator.CreateArrowhead(previewContainer.transform, previewLineMaterial);
    }

    private void UpdatePreviewLine()
    {
        if (selectedBlock == null || previewLineRenderer == null || previewContainer == null)
            return;

        if (!previewContainer.activeSelf) return;

        Vector3 startPos = selectedBlock.GetOutputPoint();

        if (!TryGetRay(out Ray ray))
            return;

        Vector3 endPos;
        if (Physics.Raycast(ray, out RaycastHit hit, maxPreviewDistance, blockLayerMask))
        {
            endPos = hit.point;
        }
        else
        {
            endPos = ray.GetPoint(maxPreviewDistance);
        }

        previewLineRenderer.SetPosition(0, startPos);
        previewLineRenderer.SetPosition(1, endPos);

        UpdateArrowhead(previewArrowhead, startPos, endPos);
    }

    private void UpdateAllConnectionLines()
    {
        for (int i = connections.Count - 1; i >= 0; i--)
        {
            ConnectionData data = connections[i];

            if (data.from == null || data.to == null)
            {
                if (data.line != null) Destroy(data.line.gameObject);
                connections.RemoveAt(i);
                continue;
            }

            Vector3 startPos = data.from.GetOutputPoint();
            Vector3 endPos = data.to.GetInputPoint();

            Debug.DrawLine(startPos, endPos, Color.yellow);

            data.line.SetPosition(0, startPos);
            data.line.SetPosition(1, endPos);

            UpdateArrowhead(data.arrowhead, startPos, endPos);
        }
    }

    private void UpdateArrowhead(GameObject arrowhead, Vector3 from, Vector3 to)
    {
        if (arrowhead == null) return;

        Vector3 direction = (to - from).normalized;
        float length = Vector3.Distance(from, to);
        Vector3 midPoint = from + direction * (length * 0.85f);

        arrowhead.transform.position = midPoint;
        arrowhead.transform.rotation = Quaternion.LookRotation(direction);
    }
}
