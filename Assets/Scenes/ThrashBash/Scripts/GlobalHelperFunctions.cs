
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GlobalHelperFunctions : UdonSharpBehaviour
{
    public const string BUILD_VERSION = "0.29.2";

    // Enum replacement helper
    public static int KeyToPowerupType(string enum_str_name)
    {
        var cleanStr = enum_str_name.Trim().ToLower();
        //var output = (int)powerup_type_name.Fallback;
        var output = 0;
        if (cleanStr == "sizeup") { output = (int)powerup_type_name.SizeUp; }
        else if (cleanStr == "sizedown") { output = (int)powerup_type_name.SizeDown; }
        else if (cleanStr == "speedup") { output = (int)powerup_type_name.SpeedUp; }
        else if (cleanStr == "atkup") { output = (int)powerup_type_name.AtkUp; }
        else if (cleanStr == "defup") { output = (int)powerup_type_name.DefUp; }
        else if (cleanStr == "atkdown") { output = (int)powerup_type_name.AtkDown; }
        else if (cleanStr == "defdown") { output = (int)powerup_type_name.DefDown; }
        else if (cleanStr == "lowgrav") { output = (int)powerup_type_name.LowGrav; }
        else if (cleanStr == "partialheal") { output = (int)powerup_type_name.PartialHeal; }
        else if (cleanStr == "fullheal") { output = (int)powerup_type_name.FullHeal; }
        else if (cleanStr == "multijump") { output = (int)powerup_type_name.Multijump; }
        else if (cleanStr == "highgrav") { output = (int)powerup_type_name.HighGrav; }
        //UnityEngine.Debug.Log("Attempted to match key '" + cleanStr + "' to value: " + output);
        return output;
    }
    public static int KeyToWeaponType(string enum_str_name)
    {
        string cleanStr = enum_str_name.Trim().ToLower();
        int output = 0;
        if (cleanStr == "punchingglove") { output = (int)weapon_type_name.PunchingGlove; }
        else if (cleanStr == "bomb") { output = (int)weapon_type_name.Bomb; }
        else if (cleanStr == "rocket") { output = (int)weapon_type_name.Rocket; }
        else if (cleanStr == "bossglove") { output = (int)weapon_type_name.BossGlove; }
        else if (cleanStr == "hyperglove") { output = (int)weapon_type_name.HyperGlove; }
        else if (cleanStr == "megaglove") { output = (int)weapon_type_name.MegaGlove; }
        else if (cleanStr == "superlaser") { output = (int)weapon_type_name.SuperLaser; }
        else if (cleanStr == "throwableitem") { output = (int)weapon_type_name.ThrowableItem; }
        return output;
    }

    // Transform helpers
    public static Quaternion RotateTowards(Vector3 source_position, Vector3 target_position)
    {
        //Quaternion rotateTo = source_rotation;
        Vector3 targetDir = (source_position - target_position).normalized;
        //Vector3 headForward = rotateTo * Vector3.forward;
        //if (noVertical) { headForward.y = 0; }
        //headForward = headForward.normalized;
        //float angleOffset = Vector3.SignedAngle(headForward, targetDir, Vector3.up);
        //Quaternion.AngleAxis(-angleOffset, Vector3.up) * 
        Quaternion rotateTo = Quaternion.LookRotation(targetDir, Vector3.up);
        return rotateTo;
    }

    public static void SetGlobalScale(Transform transform, Vector3 globalScale)
    {
        transform.localScale = Vector3.one;
        transform.localScale = new Vector3(globalScale.x / transform.lossyScale.x, globalScale.y / transform.lossyScale.y, globalScale.z / transform.lossyScale.z);
    }

    public static Transform GetChildTransformByName(Transform parent_tranform, string name)
    {
        Transform transform_out = null;
        foreach (Transform child in parent_tranform)
        {
            if (child.name.ToLower().Trim() == name.ToLower().Trim())
            {
                transform_out = child;
                break;
            }
        }
        return transform_out;
    }

    public static bool IntToBool(int i)
    {
        bool b = false;
        if (i >= 1) { b = true; }
        return b;
    }

    public static int BoolToInt(bool b)
    {
        int i = 0;
        if (b) { i = 1; }
        return i;
    }

    public static int StringToInt(string str)
    {
        int result = -404;
        int.TryParse(str, out result); // UdonSharp supports TryParse
        return result;
    }

    public static int[] ConvertStrToIntArray(string str)
    {
        if (str == "" || str == null) { return null; }
        string[] splitStr = str.Split(',');
        if (splitStr == null) { return null; }
        int[] arrOut = new int[splitStr.Length];

        for (int i = 0; i < splitStr.Length; i++)
        {
            var intAttempt = StringToInt(splitStr[i]);
            if (intAttempt != 404) { arrOut[i] = intAttempt; }
        }
        return arrOut;
    }

    // To-do: replace all references of this with a String.Join() [or alternatively, just make that what internally happens here]
    public static string ConvertIntArrayToString(int[] arrIn)
    {
        if (arrIn == null || arrIn.Length == 0) return "";

        string result = arrIn[0].ToString();
        for (int i = 1; i < arrIn.Length; i++)
        {
            result += ',';
            result += arrIn[i].ToString();
        }
        return result;
    }

    public static int[] AddToIntArray(int inValue, int[] inArr)
    {
        var arrOut = new int[inArr.Length + 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            arrOut[i] = inArr[i];
        }
        arrOut[inArr.Length] = inValue;
        return arrOut;
    }

    public static int[] RemoveIndexFromIntArray(int inIndex, int[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new int[0]; }
        var arrOut = new int[inArr.Length - 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inIndex == i) { continue; }
            if (i > inIndex) { arrOut[i - 1] = inArr[i]; }
            else if (i < arrOut.Length) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }

    public static int[] RemoveValueFromIntArray(int inValue, int[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new int[0]; }
        var arrOut = new int[inArr.Length - 1];
        var found_value = false;
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inValue == inArr[i]) { found_value = true; continue; }
            if (found_value) { arrOut[i - 1] = inArr[i]; }
            else if (i < arrOut.Length) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }


    public static int DictIndexFromKey(int key, int[] keys)
    {
        if (keys == null || keys.Length <= 0) { UnityEngine.Debug.Log("Invalid dictionary!"); return -999; }
        int key_index = -999;
        for (int i = 0; i < keys.Length; i++)
        {
            if (key == keys[i]) { key_index = i; break; }
        }
        return key_index;
    }

    public static int DictValueFromKey(int key, int[] keys, int[] values)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return -999; }
        int key_index = DictIndexFromKey(key, keys);
        if (key_index < 0) { return -999; }
        return values[key_index];
    }

    public static void DictAddEntry(int key, int value, ref int[] keys, ref int[] values)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return; }
        keys = AddToIntArray(key, keys);
        values = AddToIntArray(value, values);
    }

    public static void DictRemoveEntry(int key, ref int[] keys, ref int[] values)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return; }
        int key_index = DictIndexFromKey(key, keys);
        keys = RemoveIndexFromIntArray(key_index, keys);
        values = RemoveIndexFromIntArray(key_index, values);
    }

    public static int[][] DictFindAllWithValue(int value, int[] keys, int[] values, int compare_op = 0)
    {
        if (keys == null || keys.Length <= 0 || values == null || values.Length <= 0 || keys.Length != values.Length) { UnityEngine.Debug.Log("Invalid dictionary!"); return null; }
        int out_arr_size = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if ((compare_op == (int)dict_compare_name.Equals && values[i] == value)
            || (compare_op == (int)dict_compare_name.GreaterThan && values[i] > value)
            || (compare_op == (int)dict_compare_name.LessThan && values[i] < value)
            || (compare_op == (int)dict_compare_name.GreaterThanOrEqualsTo && values[i] >= value)
            || (compare_op == (int)dict_compare_name.LessThanOrEqualsTo && values[i] <= value))
            {
                out_arr_size++;
            }
        }
        int[] keys_out = new int[out_arr_size];
        int[] values_out = new int[out_arr_size];
        int index_iter = 0;
        for (int j = 0; j < values.Length; j++)
        {
            if ((compare_op == (int)dict_compare_name.Equals && values[j] == value)
            || (compare_op == (int)dict_compare_name.GreaterThan && values[j] > value)
            || (compare_op == (int)dict_compare_name.LessThan && values[j] < value)
            || (compare_op == (int)dict_compare_name.GreaterThanOrEqualsTo && values[j] >= value)
            || (compare_op == (int)dict_compare_name.LessThanOrEqualsTo && values[j] <= value))
            {
                keys_out[index_iter] = keys[j];
                values_out[index_iter] = values[j];
                index_iter++;
            }
        }
        int[][] dict_out = new int[2][];
        dict_out[0] = keys_out; dict_out[1] = values_out;
        //UnityEngine.Debug.Log("Dictionary output: " + ConvertIntArrayToString(keys_out) + " | " + ConvertIntArrayToString(values_out));
        return dict_out;
    }

    public static void DictSort(ref int[] keys, ref int[] values, bool ascending_sort = true, bool keys_only = false)
    {
        if (keys == null || values == null || keys.Length != values.Length) return;

        int len = keys.Length;

        for (int i = 1; i < len; i++)
        {
            int key = keys[i];
            int val = values[i];
            int j = i - 1;

            if (ascending_sort)
            {
                while (j >= 0 && values[j] > val)
                {
                    keys[j + 1] = keys[j];
                    if (!keys_only) values[j + 1] = values[j];
                    j--;
                }
            }
            else
            {
                while (j >= 0 && values[j] < val)
                {
                    keys[j + 1] = keys[j];
                    if (!keys_only) values[j + 1] = values[j];
                    j--;
                }
            }

            keys[j + 1] = key;
            if (!keys_only) values[j + 1] = val;
        }
    }

    public static bool ArraysEqual(int[] a, int[] b)
    {
        if (a == null || b == null) { return false; }
        if (a.Length != b.Length) { return false; }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) { return false; }
        }
        return true;
    }

    public static GameObject[] AddToGameObjectArray(GameObject inValue, GameObject[] inArr)
    {
        var arrOut = new GameObject[inArr.Length + 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            arrOut[i] = inArr[i];
        }
        arrOut[inArr.Length] = inValue;
        return arrOut;
    }

    public static GameObject[] RemoveEntryFromGameObjectArray(GameObject inValue, GameObject[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new GameObject[0]; }
        var arrOut = new GameObject[inArr.Length - 1];
        var found_value = false;
        for (var i = 0; i < inArr.Length; i++)
        {
            if (inValue == inArr[i]) { found_value = true; continue; }
            if (found_value) { arrOut[i - 1] = inArr[i]; }
            else if (i < arrOut.Length) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }

    public static GameObject[] RemoveIndexFromGameObjectArray(int index, GameObject[] inArr)
    {
        // If we are removing the last entry or there are no entries, just return empty array
        if (inArr.Length <= 1) { return new GameObject[0]; }
        var arrOut = new GameObject[inArr.Length - 1];
        for (var i = 0; i < inArr.Length; i++)
        {
            if (i == index) { continue; }
            else if (i > index) { arrOut[i - 1] = inArr[i]; }
            else if (i < index) { arrOut[i] = inArr[i]; }
        }
        return arrOut;
    }


    public static float[] CalcGridDistr(int item_count, int base_column_count, Vector2 item_base_dims, Vector3 item_base_scale, Vector2 grid_dims, bool rows_instead_of_columns = false)
    {
        // Outputs: column_count, scale_x, scale_y, scale_z, spacing_x, spacing_y 
        float[] out_arr = new float[6];
        if (base_column_count <= 0) { return out_arr; }

        // Get the dims that can fit in a single column
        bool found_fit = false;
        int columns_to_fit = 1;
        Vector2 grid_spacing = new Vector2(0, 0);

        float fit_dim = item_base_dims.y;
        if (rows_instead_of_columns) { fit_dim = item_base_dims.x; }

        Vector3 item_new_scale = new Vector3((float)item_base_scale.x, (float)item_base_scale.y, (float)item_base_scale.z);
        item_new_scale *= base_column_count;

        while (!found_fit)
        {
            if (rows_instead_of_columns)
            {
                // I want to fit 7 items of size 100 in a 600 grid. Base column count is 1.
                // iter 1: 700 > 600, go to next iter with a scalar of 0.5
                // iter 2: 175 <= 600, this is our stop
                fit_dim = (item_base_dims.x * item_count * item_new_scale.x * (1.0f / columns_to_fit));
                if (fit_dim > grid_dims.x)
                {
                    columns_to_fit++;
                    item_new_scale = item_base_scale * ((float)base_column_count / columns_to_fit);
                    continue;
                }
                found_fit = true;
            }
            else
            {
                // I want to fit 7 items of size 100 in a 600 grid. Base column count is 1.
                // iter 1: 700 > 600, go to next iter with a scalar of 0.5
                // iter 2: 175 <= 600, this is our stop
                fit_dim = (item_base_dims.y * item_count * item_new_scale.y * (1.0f / columns_to_fit));
                if (fit_dim > grid_dims.y)
                {
                    columns_to_fit++;
                    item_new_scale = item_base_scale * ((float)base_column_count / columns_to_fit);
                    continue;
                }
                found_fit = true;
            }
        }

        grid_spacing = item_base_dims * ((float)base_column_count / columns_to_fit);
        if (columns_to_fit <= base_column_count)
        {
            grid_spacing -= item_base_dims;
        }
        else
        {
            grid_spacing += item_base_dims;
        }
        out_arr[0] = columns_to_fit;
        out_arr[1] = item_new_scale.x;
        out_arr[2] = item_new_scale.y;
        out_arr[3] = item_new_scale.z;
        out_arr[4] = grid_spacing.x;
        out_arr[5] = grid_spacing.y;

        //return new Vector4(item_new_scale.x, item_new_scale.y, item_new_scale.z, columns_to_fit);
        return out_arr;
    }


}
