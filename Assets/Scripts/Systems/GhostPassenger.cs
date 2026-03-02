using UnityEngine;
using System.Collections;

// 電車内の幽霊乗客
// プレイヤーが一定距離に近づくとフェードアウトして消える
public class GhostPassenger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float disappearDistance = 2.2f;
    [SerializeField] private float fadeSpeed         = 3.5f;

    private Transform      playerTransform;
    private Renderer[]     renderers;
    private bool           isFading      = false;
    private float[]        originalAlpha;

    private void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p) playerTransform = p.transform;

        renderers     = GetComponentsInChildren<Renderer>();
        originalAlpha = new float[renderers.Length];

        // マテリアルを半透明モードに設定
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].material;
            SetTransparent(mat);
            // 初期は少し透明（0.55）
            var c = mat.color;
            c.a = 0.55f;
            mat.color = c;
            originalAlpha[i] = c.a;
        }
    }

    private void Update()
    {
        if (playerTransform == null || isFading) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist < disappearDistance)
        {
            isFading = true;
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float duration = 1f / fadeSpeed;

        float[] startAlpha = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startAlpha[i] = renderers[i].material.color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < renderers.Length; i++)
            {
                var mat = renderers[i].material;
                var c   = mat.color;
                c.a     = Mathf.Lerp(startAlpha[i], 0f, t);
                mat.color = c;
            }
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private static void SetTransparent(Material mat)
    {
        mat.SetFloat("_Surface",   1f);  // Transparent
        mat.SetFloat("_Blend",     0f);  // Alpha blend
        mat.SetFloat("_ZWrite",    0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
    }
}
