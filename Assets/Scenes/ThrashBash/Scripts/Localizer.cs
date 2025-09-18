
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
    [SerializeField] private TMP_Text lang_en_json;
    [SerializeField] private TMP_Text lang_fr_json;
    [SerializeField] private TMP_Text lang_jp_json;
    [SerializeField] private TMP_Text lang_es_s_json;
    [SerializeField] private TMP_Text lang_es_l_json;

    [NonSerialized] public int language_type = (int)language_type_name.English;
    [NonSerialized] public DataDictionary lang_dict;

    void Start()
    {
        SetLangDict();
    }

    public void SetLangDict()
    {
        lang_dict = CreateLangDict(language_type);
        SetStaticText();
    }

    private DataDictionary CreateLangDict(int language_type)
    {
        string json = "";
        if (language_type == (int)language_type_name.French && lang_fr_json != null) { json = lang_fr_json.text; }
        else if (language_type == (int)language_type_name.Japanese && lang_jp_json != null) { json = lang_jp_json.text; }
        else if (language_type == (int)language_type_name.SpanishEurope && lang_es_s_json != null) { json = lang_es_s_json.text; }
        else if (language_type == (int)language_type_name.SpanishLatin && lang_es_l_json != null) { json = lang_es_l_json.text; }
        else if (lang_en_json != null) { json = lang_en_json.text; }

        if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
        {
            // Deserialization succeeded! Let's figure out what we've got.
            if (result.TokenType == TokenType.DataDictionary)
            {
                Debug.Log($"Successfully deserialized as a dictionary with {result.DataDictionary.Count} items.");
                return result.DataDictionary;
            }
            else if (result.TokenType == TokenType.DataList)
            {
                Debug.Log($"Successfully deserialized as a list with {result.DataList.Count} items.");
            }
            else
            {
                // This should not be possible. If TryDeserializeFromJson returns true, this it *must* be either a dictionary or a list.
            }
        }
        else
        {
            // Deserialization failed. Let's see what the error was.
            Debug.Log($"Failed to Deserialize json {json} - {result.ToString()}");
        }

        return null;
    }

    private string TryGetText(string key, string fallback = "[?]")
    {
        if (lang_dict == null) { return fallback; }
        // This approach has a type check built in! It's functionally the same, but streamlined.
        DataToken value;
        if (lang_dict.TryGetValue(key.ToUpper(), TokenType.String, out value))
        {
            return value.String;
        }
        else
        {
            return fallback;
        }
    }

    public string FetchText(string key, string fallback="[?]", string argument_0 = "", string argument_1="", string argument_2="", string argument_3="", string argument_4="")
    {
        return TryGetText(key, fallback).Replace("$ARG0", argument_0).Replace("$ARG1", argument_1).Replace("$ARG2", argument_2).Replace("$ARG3", argument_3).Replace("$ARG4", argument_4);
    }

    public void SetStaticText()
    {
        // The big one. This is what will change the text on EVERYTHING.

    }
}
