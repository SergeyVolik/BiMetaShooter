using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bl_WeaponLoadoutUI : MonoBehaviour
{

    [SerializeField] private RectTransform BackRect;
    [SerializeField] private RectTransform[] SlotsGroups;
    private Image[] IconsImg;
    [SerializeField] private CanvasGroup Alpha;

    private int current = 0;

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        Alpha.alpha = 0;
        Alpha.gameObject.SetActive(false);
        IconsImg = new Image[SlotsGroups.Length];
        for (int i = 0; i < SlotsGroups.Length; i++)
        {
            IconsImg[i] = SlotsGroups[i].GetComponentInChildren<Image>();
        }
        IconsImg[0].CrossFadeColor(Color.black, 0.1f, true, true);
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetInitLoadout(List<bl_Gun> guns)
    {
        for (int i = 0; i < SlotsGroups.Length; i++)
        {
            IconsImg[i].canvasRenderer.SetColor(Color.white);
            if(guns[i] == null || guns[i].Info == null) { SlotsGroups[i].gameObject.SetActive(false); continue; }
            
            Image img = SlotsGroups[i].GetComponentInChildren<Image>(false);
            img.sprite = guns[i].Info.GunIcon;
        }
        BackRect.position = SlotsGroups[0].position;
        current = 0;
        IconsImg[0].canvasRenderer.SetColor(Color.black);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ReplaceSlot(int slot, bl_Gun newGun)
    {
        IconsImg[slot].sprite = newGun.Info.GunIcon;
    }

    /// <summary>
    /// 
    /// </summary>
    public void ChangeWeapon(int nextSlot)
    {
        if (!bl_GameData.Instance.ShowWeaponLoadout) return;

        StopAllCoroutines();
        StartCoroutine(ChangeSlot(nextSlot));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerator ChangeSlot(int nextSlot)
    {
        int cacheActual = current;
        current = nextSlot;
        Alpha.gameObject.SetActive(true);
        while (Alpha.alpha < 1)
        {
            Alpha.alpha += Time.deltaTime * 4;
            yield return null;
        }
        float d = 0;
        IconsImg[cacheActual].CrossFadeColor(Color.white, 0.3f, true, true);
        IconsImg[nextSlot].CrossFadeColor(Color.black, 0.3f, true, true);
        while (d < 1)
        {
            d += Time.deltaTime * 7;
            BackRect.position = Vector3.Lerp(SlotsGroups[cacheActual].position, SlotsGroups[nextSlot].position, d);
            yield return null;
        }
        yield return new WaitForSeconds(2.5f);
        while (Alpha.alpha > 0)
        {
            Alpha.alpha -= Time.deltaTime * 4;
            yield return null;
        }
        Alpha.gameObject.SetActive(false);
    }
}