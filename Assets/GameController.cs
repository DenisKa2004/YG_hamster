using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public Animator panelAnimator;
    public GameObject button;

    public Button clickButton;
    public Animator animator;
    public Animator x2;
    public ParticleSystem particleSystem;
    public List<Sprite> sprites;
    public TextMeshProUGUI speedText;
    public GameObject _x2;
    public TextMeshProUGUI coinText;
    public Image fillImage; // Reference to the Image component for Fill Amount

    public List<Enterprise> enterprises;
    public List<Button> enterpriseButtons;

    private float clickCount = 0;
    private float clickSpeed = 0;
    private float lastClickTime = 0;
    private int coinCount = 0;
    private bool isDoubleClickMode = false;
    private bool isRefilling = false;

    public int maxParticlesPerSecond = 10; // Maximum number of particles per second

    void Start()
    {
        clickButton.onClick.AddListener(OnButtonClick);
        animator.SetBool("isClick", true);
        InitializeEnterprises();
        AssignButtonListeners();
        StartCoroutine(PassiveIncomeCoroutine());
    }

    void Update()
    {
        UpdateEnterpriseButtons();
    }

    public void Click_panel()
    {
        if (panelAnimator != null)
        {
            button.SetActive(false);
            panelAnimator.SetBool("isOpen", true);
            panelAnimator.SetBool("isClose", false);
        }
    }

    public void Close_panel()
    {
        if (panelAnimator != null)
        {
            panelAnimator.SetBool("isOpen", false);
            panelAnimator.SetBool("isClose", true);
            button.SetActive(true);
        }
    }

    void OnButtonClick()
    {
        StartCoroutine(HandleButtonClick());
    }

    IEnumerator HandleButtonClick()
    {
        animator.SetBool("isClick", false);

        int randomIndex = Random.Range(0, sprites.Count);
        var main = particleSystem.main;
        main.startLifetime = 3.0f;

        var textureSheetAnimation = particleSystem.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.mode = ParticleSystemAnimationMode.Sprites;

        textureSheetAnimation.RemoveSprite(0);
        textureSheetAnimation.AddSprite(sprites[randomIndex]);

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.startLifetime = 3.0f;
        particleSystem.Emit(emitParams, 1);

        clickCount++;

        // Increase coin count by 1 per click, or 2 in double click mode
        coinCount += isDoubleClickMode ? 2 : 1;
        coinText.text = coinCount.ToString();

        // Decrease Fill Amount only if not refilling
        if (!isRefilling)
        {
            fillImage.fillAmount -= 0.008f;
            if (fillImage.fillAmount <= 0)
            {
                fillImage.fillAmount = 0;
                _x2.SetActive(true);
                isDoubleClickMode = true;
                x2.SetBool("is2x", true);
                StartCoroutine(RefillFillAmount());
            }
        }

        float timeSinceLastClick = Time.time - lastClickTime;
        if (timeSinceLastClick > 0)
        {
            clickSpeed = 1 / timeSinceLastClick;
            speedText.text = clickSpeed.ToString("F2") + " clicks/sec";
        }
        lastClickTime = Time.time;

        // Wait for the animation to finish (assuming 0.5 seconds here)
        yield return new WaitForSeconds(0.1f);

        animator.SetBool("isClick", true);
    }

    IEnumerator RefillFillAmount()
    {
        isRefilling = true;
        while (fillImage.fillAmount < 1.0f)
        {
            fillImage.fillAmount += 0.1f;
            yield return new WaitForSeconds(0.5f);
        }
        isRefilling = false;
        isDoubleClickMode = false;
        x2.SetBool("is2x", false);
        _x2.SetActive(false);
    }

    void InitializeEnterprises()
    {
        enterprises = new List<Enterprise>
        {
            new Enterprise("Mini Farm", 100, 1),
            new Enterprise("Bakery", 500, 5),
            new Enterprise("School", 2000, 20),
            new Enterprise("Toy Factory", 10000, 100),
            new Enterprise("Wheel Factory", 50000, 500),
            new Enterprise("Research Center", 250000, 2500),
            new Enterprise("Space Station", 1000000, 10000)
        };
    }

    void AssignButtonListeners()
    {
        for (int i = 0; i < enterpriseButtons.Count; i++)
        {
            int index = i;
            enterpriseButtons[i].onClick.AddListener(() => PurchaseEnterprise(index));
        }
    }

    void UpdateEnterpriseButtons()
    {
        string nameTemplate = "{0}";
        string costTemplate = "Cost: {0}";
        string incomeTemplate = "Income: {0}/sec";
        string levelTemplate = "Level: {0}";

        for (int i = 0; i < enterpriseButtons.Count; i++)
        {
            var button = enterpriseButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            button.text = string.Format("{0}\n{1}\n{2}\n{3}",
                                        string.Format(nameTemplate, enterprises[i].name),
                                        string.Format(costTemplate, enterprises[i].cost),
                                        string.Format(incomeTemplate, enterprises[i].income),
                                        string.Format(levelTemplate, enterprises[i].level));
        }
    }

    void PurchaseEnterprise(int index)
    {
        Enterprise enterprise = enterprises[index];
        bool canPurchase = (index == 0) || (enterprises[index - 1].level > 0); // Ensure previous enterprise is unlocked

        Debug.Log($"Attempting to purchase {enterprise.name}");
        Debug.Log($"Can Purchase: {canPurchase}, Coin Count: {coinCount}, Cost: {enterprise.cost}, Level: {enterprise.level}");

        if (coinCount >= enterprise.cost && enterprise.level < 10 && canPurchase)
        {
            coinCount -= enterprise.cost;
            enterprise.level++;
            enterprise.cost = Mathf.RoundToInt(enterprise.cost * 1.5f);
            enterprise.income += Mathf.RoundToInt(enterprise.income * 0.1f);

            Image enterpriseImage = enterpriseButtons[index].transform.GetChild(2).GetComponent<Image>();
            if (enterpriseImage != null)
            {
                enterpriseImage.color = Color.white;
            }

            Debug.Log($"{enterprise.name} purchased successfully. New Level: {enterprise.level}, New Cost: {enterprise.cost}, New Income: {enterprise.income}");
        }
        else
        {
            Debug.Log($"Cannot purchase {enterprise.name}. Conditions not met.");
        }
    }

    IEnumerator PassiveIncomeCoroutine()
    {
        while (true)
        {
            int passiveIncome = 0;
            foreach (var enterprise in enterprises)
            {
                passiveIncome += enterprise.income * enterprise.level;
            }

            coinCount += passiveIncome;
            coinText.text = coinCount.ToString();

            // Emit particles gradually for passive income
            StartCoroutine(EmitParticlesGradually(passiveIncome));

            // Calculate the click speed including passive income
            float timeSinceLastClick = Time.time - lastClickTime;
            if (timeSinceLastClick > 0)
            {
                clickSpeed = (1 / timeSinceLastClick) + passiveIncome;
                speedText.text = clickSpeed.ToString("F2") + " clicks/sec";
            }

            lastClickTime = Time.time;
            yield return new WaitForSeconds(1);
        }
    }

    IEnumerator EmitParticlesGradually(int count)
    {
        int particlesToEmit = Mathf.Min(count, maxParticlesPerSecond);
        for (int i = 0; i < particlesToEmit; i++)
        {
            particleSystem.Emit(1);
            yield return new WaitForSeconds(1.0f / maxParticlesPerSecond);
        }
    }
}

[System.Serializable]
public class Enterprise
{
    public string name;
    public int cost;
    public int income;
    public int level;

    public Enterprise(string name, int cost, int income)
    {
        this.name = name;
        this.cost = cost;
        this.income = income;
        this.level = 0;
    }
}
