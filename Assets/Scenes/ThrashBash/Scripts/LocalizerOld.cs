/*
using BestHTTP.JSON;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public enum language_type_name
{
    English, French, Japanese, SpanishLatin, SpanishEurope
}

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Localizer : UdonSharpBehaviour
{
    [SerializeField] private VRCUrl lang_en_url;
    [SerializeField] private VRCUrl lang_fr_url;
    [SerializeField] private VRCUrl lang_jp_url;
    [SerializeField] private VRCUrl lang_es_s_url;
    [SerializeField] private VRCUrl lang_es_l_url;

    [NonSerialized] public int language_type = (int)language_type_name.English;
    [NonSerialized] public bool lang_file_ready = false;
    [NonSerialized] public double lang_file_load_attempt_start_ms = 0.0f;
    [NonSerialized] public byte lang_file_load_attempt_count = 0;
    [NonSerialized] public const byte MAX_LANG_FILE_LOAD_ATTEMPTS = 4;
    [NonSerialized] public const byte LANG_FILE_TIMEOUT_SECONDS = 60;

    [NonSerialized] public DataDictionary lang_dict;

    void Start()
    {
        FetchLanguageAttempt(true);
    }

    private void Update()
    {
        // If our language file isn't ready and we haven't already maximized the # of attempts to load it, try to load it again
        if (!lang_file_ready && lang_file_load_attempt_count < MAX_LANG_FILE_LOAD_ATTEMPTS && lang_file_load_attempt_start_ms != 0.0f)
        {
            // Make sure to only make attempts after a certain timeout duration. No need to spam outgoing network calls.
            double load_timer = (float)Networking.CalculateServerDeltaTime(Networking.GetServerTimeInSeconds(), lang_file_load_attempt_start_ms);
            if (load_timer > LANG_FILE_TIMEOUT_SECONDS)
            {
                FetchLanguageAttempt(false);
            }
        }
    }

    public void FetchLanguageAttempt(bool reset = false)
    {
        lang_file_ready = false;
        if (reset) { lang_file_load_attempt_count = 0; }
        else { lang_file_load_attempt_count++; }
        lang_file_load_attempt_start_ms = Networking.GetServerTimeInSeconds();
        VRCStringDownloader.LoadUrl(GetURLFromLanguage(language_type), (IUdonEventReceiver)this);
    }

    private VRCUrl GetURLFromLanguage(int language_id)
    {
        if (language_id == (int)language_type_name.French) { return lang_fr_url; }
        else if (language_id == (int)language_type_name.Japanese) { return lang_jp_url; }
        else if (language_id == (int)language_type_name.SpanishLatin) { return lang_es_l_url; }
        else if (language_id == (int)language_type_name.SpanishEurope) { return lang_es_s_url; }
        return lang_en_url; 
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        string resultAsUTF8 = result.Result;
        lang_file_load_attempt_start_ms = 0.0f;
        lang_file_load_attempt_count = 0;
        lang_file_ready = true;

        if (VRCJson.TryDeserializeFromJson(result.Result, out DataToken JSONresult))
        {
            // Deserialization succeeded! Let's figure out what we've got.
            if (JSONresult.TokenType == TokenType.DataDictionary)
            {
                Debug.Log($"Successfully deserialized as a dictionary with {JSONresult.DataDictionary.Count} items.");
                lang_dict = JSONresult.DataDictionary;
                //LocalizeElements(JSONresult.DataDictionary);
            }
            else if (JSONresult.TokenType == TokenType.DataList)
            {
                Debug.Log($"Successfully deserialized as a list with {JSONresult.DataList.Count} items.");
            }
            else
            {
                // This should not be possible. If TryDeserializeFromJson returns true, input *must* be either a dictionary or a list.
            }
        }
        else
        {
            // Deserialization failed. Let's see what the error was.
            Debug.Log($"Failed to Deserialize json {result.Result} - {result.ToString()}");
        }

    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError($"Error loading string: {result.ErrorCode} - {result.Error}");
    }

    public string FetchText(string key)
    {
        if (lang_dict == null) { return "[?]"; }
        // The big one. This is what will change the text on EVERYTHING.
        // This approach has a type check built in! It's functionally the same, but streamlined.
        DataToken value;
        if (lang_dict.TryGetValue("key", TokenType.String, out value))
        {
            return value.String;
        }
        else
        {
            return "[?]";
        }
    }
}
*/