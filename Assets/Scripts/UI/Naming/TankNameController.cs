using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TankNameController : MonoBehaviour
{
    [SerializeField, Tooltip("All possible tank name combinations")] private TankNames allTankNames;

    [SerializeField, Tooltip("The text showing the article of the name.")] private TextMeshProUGUI articleText;
    [SerializeField, Tooltip("The text showing the adjective of the name.")] private TextMeshProUGUI adjectiveText;
    [SerializeField, Tooltip("The text showing the noun of the name.")] private TextMeshProUGUI nounText;

    private int currentArticle;
    private int currentAdjective;
    private int currentNoun;

    private void Start()
    {
        RandomizeName();
    }

    public void RandomizeName()
    {
        RandomizeArticle();
        RandomizeAdjective();
        RandomizeNoun();
    }

    public void ChangeArticle(int increment)
    {
        currentArticle += increment;

        if (currentArticle < 0)
            currentArticle = allTankNames.articles.Length - 1;

        if (currentArticle >= allTankNames.articles.Length)
            currentArticle = 0;

        SetArticleText();
    }

    public void ChangeAdjective(int increment)
    {
        currentAdjective += increment;

        if (currentAdjective < 0)
            currentAdjective = allTankNames.adjectives.Length - 1;

        if (currentAdjective >= allTankNames.adjectives.Length)
            currentAdjective = 0;

        SetAdjectiveText();
    }

    public void ChangeNoun(int increment)
    {
        currentNoun += increment;

        if (currentNoun < 0)
            currentNoun = allTankNames.nouns.Length - 1;

        if (currentNoun >= allTankNames.nouns.Length)
            currentNoun = 0;

        SetNounText();
    }

    public void RandomizeArticle()
    {
        currentArticle = Random.Range(0, allTankNames.articles.Length);
        SetArticleText();
    }
    public void RandomizeAdjective()
    {
        currentAdjective = Random.Range(0, allTankNames.adjectives.Length);
        SetAdjectiveText();
    }
    public void RandomizeNoun()
    {
        currentNoun = Random.Range(0, allTankNames.nouns.Length);
        SetNounText();
    }

    private void SetArticleText() => articleText.text = allTankNames.articles[currentArticle];
    private void SetAdjectiveText() => adjectiveText.text = allTankNames.adjectives[currentAdjective];
    private void SetNounText() => nounText.text = allTankNames.nouns[currentNoun];

    public string GetCurrentName() => allTankNames.articles[currentArticle] + " " + allTankNames.adjectives[currentAdjective] + " " + allTankNames.nouns[currentNoun];
}
