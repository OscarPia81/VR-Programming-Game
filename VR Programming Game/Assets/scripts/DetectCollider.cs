using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DetectCollider : MonoBehaviour
{
    public Collider Detect;
    private bool hasConnected = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.name.Contains("Floor") || other.transform.root.name.Contains("Floor")) return;
        
        if (hasConnected) return;

        Code myCode = transform.parent.GetComponent<Code>();
        Code otherCode = other.GetComponentInParent<Code>();

        if (myCode == null || otherCode == null || myCode == otherCode) return;
        if (otherCode is WhileCode || otherCode is IfCode) return;
        if (myCode is WhileCode || myCode is IfCode) return;
        if (otherCode.transform.IsChildOf(myCode.transform)) return;
        if (myCode.transform.IsChildOf(otherCode.transform)) return;
        if (myCode.next != null) return;

        XRGrabInteractable myGrab = transform.parent.GetComponent<XRGrabInteractable>();
        if (myGrab != null && myGrab.isSelected) return;

        XRGrabInteractable otherGrab = otherCode.GetComponent<XRGrabInteractable>();
        if (otherGrab != null && otherGrab.isSelected) return;

        if (!other.CompareTag("CodeBlock") && !otherCode.CompareTag("CodeBlock")) return;

        Debug.Log($"[Detect] {myCode.name} -> {otherCode.name}");
        ConnectCode(myCode, otherCode);
    }

    private void ConnectCode(Code self, Code otherCode)
    {
        Rigidbody otherRb = otherCode.GetComponent<Rigidbody>();
        Collider selfCol = self.GetComponent<Collider>();
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

        if (otherCol != null)
        {
            otherCol.isTrigger = false;
        }

        self.next = otherCode;
        Vector3 worldPos = otherCode.transform.position;
        otherCode.transform.SetParent(self.transform, true);
        otherCode.transform.position = worldPos;

        hasConnected = true;
        StartCoroutine(SmoothConnect(otherCode, self.transform));
    }

    private IEnumerator SmoothConnect(Code otherCode, Transform parent)
    {
        Vector3 worldTargetPos = parent.TransformPoint(new Vector3(-1f, 0, 0));
        Vector3 startPos = otherCode.transform.position;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            otherCode.transform.position = Vector3.Lerp(startPos, worldTargetPos, t);
            yield return null;
        }

        otherCode.transform.position = worldTargetPos;
        Physics.SyncTransforms();
    }
}