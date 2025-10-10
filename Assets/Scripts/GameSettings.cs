using SimpleJSON;
using System;

[Serializable]
public class GameSettings : Settings
{
    public int exitType = 0; //0: default none login, 1: exit back to roWeb/previous page, 3: restart game
    public int qa_font_alignment = 1;
    public int playerNumber = 0;
    public int retryTimes;
    public int pass_accuracy_score;
    public int pass_pron_score;
    public int eachQAMarks = 0;
}

public static class SetParams
{
    public static void setCustomParameters(GameSettings settings = null, JSONNode jsonNode= null)
    {
        if (settings != null && jsonNode != null)
        {
            ////////Game Customization params/////////
            settings.playerNumber = jsonNode["setting"]["player_number"] != null ? jsonNode["setting"]["player_number"] : 4;
            settings.retryTimes = jsonNode["setting"]["retry_times"] != null ? jsonNode["setting"]["retry_times"] : null;
            settings.pass_accuracy_score = jsonNode["setting"]["pass_accuracy_score"] != null ? jsonNode["setting"]["pass_accuracy_score"] : 60;
            settings.pass_pron_score = jsonNode["setting"]["pass_pron_score"] != null ? jsonNode["setting"]["pass_pron_score"] : 60;

            LoaderConfig.Instance.gameSetup.playerNumber = settings.playerNumber;
            LoaderConfig.Instance.gameSetup.retry_times = settings.retryTimes;
            LoaderConfig.Instance.gameSetup.passAccuracyScore = settings.pass_accuracy_score;
            LoaderConfig.Instance.gameSetup.passPronScore = settings.pass_pron_score;

            if (jsonNode["setting"]["qa_font_alignment"] != null)
            {
                settings.qa_font_alignment = jsonNode["setting"]["qa_font_alignment"];
                LoaderConfig.Instance.gameSetup.qa_font_alignment = settings.qa_font_alignment;
            }

            if (jsonNode["setting"]["exit_type"] != null)
            {
                settings.exitType = jsonNode["setting"]["exit_type"];
                LoaderConfig.Instance.gameSetup.gameExitType = settings.exitType;
            }

            if (jsonNode["setting"]["score"] != null)
            {
                settings.eachQAMarks = jsonNode["setting"]["score"];
                LoaderConfig.Instance.gameSetup.gameSettingScore = settings.eachQAMarks;
            }

        }
    }
}

