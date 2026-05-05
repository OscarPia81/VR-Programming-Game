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
        
        BoxCollider myCollider = GetComponent<BoxCollider>();
        Vector3 myPos = transform.position;
        Vector3 otherPos = other.transform.position;
        Vector3 mySize = myCollider != null ? myCollider.size : Vector3.zero;
        
        Debug.Log($"[DetectCollider] 触发: {other.name}, 我的父: {transform.parent.name}, " +
                  $"我的位置: {myPos}, 其他位置: {otherPos}, 距离: {Vector3.Distance(myPos, otherPos):F2}, " +
                  $"我的碰撞体大小: {mySize}");
        
        if (hasConnected) 
        {
            Debug.Log($"[DetectCollider] {transform.parent.name} 已连接，跳过");
            return;
        }

        Code myCode = transform.parent.GetComponent<Code>();
        Code otherCode = other.GetComponentInParent<Code>();

        if (myCode == null || otherCode == null || myCode == otherCode) return;

        if (myCode.next != null) 
        {
            Debug.Log($"[DetectCollider] {myCode.name} 已有next: {myCode.next.name}，跳过");
            return;
        }

        XRGrabInteractable myGrab = transform.parent.GetComponent<XRGrabInteractable>();
        if (myGrab != null && myGrab.isSelected) 
        {
            Debug.Log($"[DetectCollider] {transform.parent.name} 正在被选中，跳过");
            return;
        }

        XRGrabInteractable otherGrab = otherCode.GetComponent<XRGrabInteractable>();
        if (otherGrab != null && otherGrab.isSelected) 
        {
            Debug.Log($"[DetectCollider] {otherCode.name} 正在被选中，跳过");
            return;
        }

        if (!other.CompareTag("CodeBlock") && !otherCode.CompareTag("CodeBlock")) 
        {
            Debug.Log($"[DetectCollider] 不是CodeBlock标签，跳过");
            return;
        }

        Debug.Log($"[DetectCollider] 尝试连接 {myCode.name} → {otherCode.name}");
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