using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{   

    [SerializeField] private GameObject[] UIpages;
    // Start is called before the first frame update
    public void hideAllPages()
    {
        foreach (GameObject page in UIpages)
        {
            page.SetActive(false);
        }
    }  

    public void showPage(int pageIndex)
    {
        hideAllPages();
        UIpages[pageIndex].SetActive(true);
    }


    


}
