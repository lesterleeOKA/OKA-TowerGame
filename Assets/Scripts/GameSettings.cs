using SimpleJSON;
using System;

[Serializable]
public class GameSettings : Settings
{
    public int exitType = 0; //0: default none login, 1: exit back to roWeb/previous page, 3: restart game
    public int qa_font_alignment = 1;
    public int playerNumber = 0;
    public int retryTimes;
    public float player_speed = 0;
    public string[] object_item_images = null;
    public int eachQAMarks = 0;
    //public string normal_color;
    //public string pressed_color;
}

public static class SetParams
{
    public static void setCustomParameters(GameSettings settings = null, JSONNode jsonNode= null)
    {
        if (settings != null && jsonNode != null)
        {
            ////////Game Customization params/////////
            var jsonArray = jsonNode["setting"]["object_item_images"].AsArray;
            settings.retryTimes = jsonNode["setting"]["retry_times"] != null ? jsonNode["setting"]["retry_times"] : null;
            if (jsonNode["setting"]["retry_times"] != null)
            {
                settings.retryTimes = jsonNode["setting"]["retry_times"];
                LoaderConfig.Instance.gameSetup.retry_times = settings.retryTimes;
            }

            if (jsonArray != null)
            {
                settings.object_item_images = new string[jsonArray.Count];
                for (int i = 0; i < jsonArray.Count; i++)
                {
                    var objectItemImages = jsonArray[i].ToString().Replace("\"", "");
                    if (!objectItemImages.StartsWith("https://") || !objectItemImages.StartsWith(APIConstant.blobServerRelativePath))
                        settings.object_item_images[i] = APIConstant.blobServerRelativePath + objectItemImages;
                }
            }
            if (jsonNode["setting"]["qa_font_alignment"] != null)
            {
                settings.qa_font_alignment = jsonNode["setting"]["qa_font_alignment"];
                LoaderConfig.Instance.gameSetup.qa_font_alignment = settings.qa_font_alignment;
            }

            if (jsonNode["setting"]["player_speed"] != null)
            {
                settings.player_speed = jsonNode["setting"]["player_speed"];
                LoaderConfig.Instance.gameSetup.playersMovingSpeed = settings.player_speed;
            }

            if (jsonNode["setting"]["player_number"] != null)
            {
                settings.playerNumber = jsonNode["setting"]["player_number"];
                LoaderConfig.Instance.gameSetup.playerNumber = settings.playerNumber;
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

