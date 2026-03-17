using Fusion;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmojiManager : NetworkBehaviour
{
    public ParticleSystem partical; // Reference to the ParticleSystem
    public Transform spawnPoint; // Point where particles will be spawned
    public EmojiScriptable emojiSO;   // Array of emoji sprites to choose from
    public GameObject emojiPrefab;
    public Transform emojiParent;
    public Image emoji;
    private ParticleSystemRenderer particleRenderer; // Renderer for the ParticleSystem
    private Material particleMaterial;               // New material instance for the ParticleSystem
    public int emojiIndex; // Index of the current emoji sprite

    void Start()
    {
        if (Object.HasInputAuthority)
        {
            Init();
        }
    }

    public void Init()
    {
        for (int i = 0; i < emojiSO.emojiData.Count; i++)
        {
            GameObject eb = Instantiate(emojiPrefab, emojiParent);
            eb.GetComponentInChildren<TMP_Text>().text = emojiSO.emojiData[i].emojiSprite.name;
            eb.GetComponentInChildren<Image>().sprite = emojiSO.emojiData[i].emojiSprite;
            int ind = i;
            eb.GetComponent<Button>().onClick.AddListener(() => { ShowEmojiRPCcall(ind); });
        }
    }

    void Update()
    {
        if (ProjectManager.instance.platformAndSettings.platformChoice == platform.Desktop)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (emojiIndex < (emojiSO.emojiData.Count - 1))
            {
                emojiIndex++;
            }
            else
            {
                emojiIndex = 0; // Loop back to the first emoji
            }
            ShowEmoji(emojiIndex);
        }
    }


    public void ShowEmoji(int index)
    {
        emojiIndex = index;
        if (partical == null)
        {
            Debug.LogWarning("ParticleSystem is not assigned.");
            return;
        }
        CancelInvoke(nameof(HideEmoji));
        Invoke(nameof(HideEmoji), 10f);
        emoji.gameObject.SetActive(true);
        emoji.sprite = emojiSO.emojiData[index].emojiSprite;
        ParticleSystem ps = Instantiate(partical, spawnPoint.transform.position, Quaternion.identity);
        particleRenderer = ps.GetComponent<ParticleSystemRenderer>();
        particleMaterial = new Material(particleRenderer.material);
        particleMaterial.mainTexture = emojiSO.emojiData[index].emojiSprite.texture;
        particleRenderer.material = particleMaterial;
        ps.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        ps.Play();
        Debug.Log("Spawned particles with current emoji.");
    }

    public void HideEmoji()
    {
        emoji.gameObject.SetActive(false);
    }

    public void ShowEmojiRPCcall(int index)
    {
        RPC_ShowEmoji(index);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ShowEmoji(int index)
    {
        ShowEmoji(index);
    }
}