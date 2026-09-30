using UnityEngine;
public class HumanCharacterBuilder : MonoBehaviour {
    [ContextMenu("Build Human Mesh")]
    public void Build() {
        foreach (Transform child in transform) {
            DestroyImmediate(child.gameObject);
        }
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(transform);
        body.transform.localPosition = new Vector3(0, 1, 0);
        body.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(transform);
        head.transform.localPosition = new Vector3(0, 1.8f, 0);
        head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        armL.name = "Arm_L";
        armL.transform.SetParent(transform);
        armL.transform.localPosition = new Vector3(-0.4f, 1.3f, 0);
        armL.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
        armL.transform.localRotation = Quaternion.Euler(0, 0, 75f);

        GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        armR.name = "Arm_R";
        armR.transform.SetParent(transform);
        armR.transform.localPosition = new Vector3(0.4f, 1.3f, 0);
        armR.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
        armR.transform.localRotation = Quaternion.Euler(0, 0, -75f);

        GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        legL.name = "Leg_L";
        legL.transform.SetParent(transform);
        legL.transform.localPosition = new Vector3(-0.15f, 0.4f, 0);
        legL.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);

        GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        legR.name = "Leg_R";
        legR.transform.SetParent(transform);
        legR.transform.localPosition = new Vector3(0.15f, 0.4f, 0);
        legR.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);
    }
}
