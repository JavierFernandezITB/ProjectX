using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class LightUpdater : ServicesReferences
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void Awake()
    {
        base.GetServices();
    }
    // Update is called once per frame
    void Update()
    {
        networkService.UpdatePlayerData();
        GetComponent<TMP_Text>().text =networkService.localPlayer.lightCurrency.ToString();
    }
}
