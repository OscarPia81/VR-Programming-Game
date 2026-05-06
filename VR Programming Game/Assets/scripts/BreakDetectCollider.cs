using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BreakDetectCollider : MonoBehaviour
{
    private bool hasConnected = false;

    private void OnTriggerStay(Collider other)
    {
        if (hasConnected) return;

        WhileCode parentWhile = transform.parent.GetComponentInParent<WhileCode>();
        Code otherCode = other.GetComponentInParent<Code>();

        if (parentWhile == null || otherCode == null || parentWhile.gameObject == otherCode.gameObject)
        {
            return;
        }

        if (parentWhile.next != null)
        {
            return;
        }

        if (!other.CompareTag("CodeBlock") && !otherCode.CompareTag("CodeBlock"))
        {
            return;
        }

        XRGrabInteractable myGrab = transform.parent.GetComponent<XRGrabInteractable>();
        if (myGrab != null && myGrab.isSelected) return;

        XRGrabInteractable otherGrab = otherCode.GetComponent<XRGrabInteractable>();
        if (otherGrab != null && otherGrab.isSelected) return;

        Debug.Log($"[Break] trigger: other={other.name}");
        ConnectNext(parentWhile, otherCode);
    }

    private void OnTriggerExit(Collider other)
    {
        hasConnected = false;
    }

    private void ConnectNext(WhileCode parentWhile, Code otherCode)
    {
        Rigidbody otherRb = otherCode.GetComponent<Rigidbody>();
        Collider selfCol = parentWhile.GetComponent<Collider>();
        Collider otherCol = otherCode.GetComponent<Collider>();

        if (selfCol != null && otherCol != null)
        {
            Physics.IgnoreCollision(selfCol, otherCol, true);
        }

        if (otherRb != null)
        {
            otherRb.isKinematic = true;
            otherRb.useGravity = false;
            otherRb.detectCollisions = false;
        }

        Rigidbody parentWhileRb = parentWhile.GetComponent<Rigidbody>();
        if (parentWhileRb != null)
        {
            parentWhileRb.isKinematic = true;
            parentWhileRb.useGravity = false;
        }

        Debug.Log($"[Break] parentWhile={parentWhile.name} pos={parentWhile.transform.position} lossyScale={parentWhile.transform.lossyScale} kin={parentWhileRb?.isKinematic}");
        Debug.Log($"[Break] otherCode={otherCode.name} pos={otherCode.transform.position} lossyScale={otherCode.transform.lossyScale}");

        if (otherCol != null)
        {
            otherCol.isTrigger = false;
        }

        parentWhile.next = otherCode;

        Vector3 worldPos = otherCode.transform.position;

        Transform backTransform = parentWhile.transform.Find("Back");

        if (backTransform != null)
        {
            otherCode.transform.SetParent(backTransform, true);
        }
        else
        {
            otherCode.transform.SetParent(parentWhile.transform, true);
        }

        otherCode.transform.position = worldPos;

        Debug.Log($"[Break] after SetParent: otherCode localPos={otherCode.transform.localPosition} parentWhile pos={parentWhile.transform.position}");

        var childCols = otherCode.GetComponentsInChildren<Collider>();
        var parentCols = parentWhile.GetComponentsInChildren<Collider>();
        foreach (var a in childCols)
            foreach (var b in parentCols)
                Physics.IgnoreCollision(a, b, true);

        hasConnected = true;
        StartCoroutine(SmoothConnect(parentWhile, otherCode));

        ConnectionController controller = parentWhile.GetComponentInChildren<ConnectionController>();
        if (controller != null)
        {
            controller.Refresh();
        }
    }

    private IEnumerator SmoothConnect(WhileCode parentWhile, Code otherCode)
    {
        Vector3 worldTargetPos = parentWhile.transform.TransformPoint(new Vector3(-1f, 0, 0));
        Vector3 startPos = otherCode.transform.position;
        float duration = 0.15f;
        float elapsed = 0f;

        Debug.Log($"[Break] SmoothConnect START: {otherCode.name} start={startPos} target={worldTargetPos} parentWhile pos={parentWhile.transform.position}");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            otherCode.transform.position = Vector3.Lerp(startPos, worldTargetPos, t);
            yield return null;
        }

        otherCode.transform.position = worldTargetPos;
        Physics.SyncTransforms();
        Debug.Log($"[Break] SmoothConnect END: {otherCode.name} pos={otherCode.transform.position} parentWhile pos={parentWhile.transform.position}");
    }
}