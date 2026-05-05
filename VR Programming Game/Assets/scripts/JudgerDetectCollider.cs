using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class JudgerDetectCollider : MonoBehaviour
{
    public Collider Detect;
    private bool hasConnected = false;

    private void OnTriggerStay(Collider other)
    {
        if (hasConnected) return;

        Code parentBlock = transform.parent.GetComponent<Code>();
        if (parentBlock == null) return;

        if (GetJudger() != null) return;

        BoolCode incomingJudger = other.GetComponentInParent<BoolCode>();
        if (incomingJudger == null || incomingJudger.gameObject == transform.parent.gameObject) return;

        XRGrabInteractable judgerGrab = incomingJudger.GetComponent<XRGrabInteractable>();
        if (judgerGrab != null && judgerGrab.isSelected)
        {
            return;
        }

        if (!other.CompareTag("BoolCodeBlock") && !incomingJudger.CompareTag("BoolCodeBlock")) return;

        ConnectJudger(incomingJudger);
    }

    private void ConnectJudger(BoolCode judger)
    {
        Transform parentTransform = transform.parent;
        Rigidbody judgerRb = judger.GetComponent<Rigidbody>();

        Collider parentCol = parentTransform.GetComponent<Collider>();
        Collider judgerCol = judger.GetComponent<Collider>();

        if (parentCol != null && judgerCol != null)
        {
            Physics.IgnoreCollision(parentCol, judgerCol, true);
        }

        if (judgerRb != null)
        {
            judgerRb.isKinematic = true;
            judgerRb.useGravity = false;
            judgerRb.detectCollisions = false;
        }

        if (judgerCol != null)
        {
            judgerCol.isTrigger = false;
        }

        SetJudger(judger);

        Vector3 worldPos = judger.transform.position;
        judger.transform.SetParent(parentTransform, true);
        judger.transform.position = worldPos;

        hasConnected = true;
        StartCoroutine(SmoothConnect(judger, parentTransform));
    }

    private IEnumerator SmoothConnect(BoolCode judger, Transform parent)
    {
        Vector3 worldTargetPos = parent.TransformPoint(new Vector3(0, 0, -1));
        Vector3 startPos = judger.transform.position;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            judger.transform.position = Vector3.Lerp(startPos, worldTargetPos, t);
            yield return null;
        }

        judger.transform.position = worldTargetPos;
        Physics.SyncTransforms();
    }

    private BoolCode GetJudger()
    {
        IfCode ifCode = transform.parent.GetComponent<IfCode>();
        if (ifCode != null) return ifCode.Judger;
        
        WhileCode whileCode = transform.parent.GetComponent<WhileCode>();
        if (whileCode != null) return whileCode.Judger;
        
        return null;
    }
    
    private void SetJudger(BoolCode judger)
    {
        IfCode ifCode = transform.parent.GetComponent<IfCode>();
        if (ifCode != null)
        {
            ifCode.Judger = judger;
            return;
        }
        
        WhileCode whileCode = transform.parent.GetComponent<WhileCode>();
        if (whileCode != null)
        {
            whileCode.Judger = judger;
        }
    }
}