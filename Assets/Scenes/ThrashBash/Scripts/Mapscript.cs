
using System;
using System.ComponentModel;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;


public enum mapscript_component_name
{
    bouncepad, item_spawn, spawnzone, ENUM_LENGTH
}
public class Mapscript : UdonSharpBehaviour
{
    [SerializeField] public string map_name;
    [SerializeField] public string map_description;
    [SerializeField] public Sprite map_image;
    [SerializeField] public float map_snd_radius;
    [SerializeField] public float map_gravity_scale = 1.0f;
    [SerializeField] public byte min_players_to_extend_room = 12;
    [SerializeField] public int voice_distance = 500;
    [SerializeField] public Texture skybox_tex;
    [SerializeField] public Transform map_readyroom_center;
    [SerializeField] public AudioClip[] snd_game_music_clips;
    [SerializeField] public AudioClip[] snd_boss_music_clips;
    [SerializeField] public AudioClip[] snd_infection_music_clips;
    [SerializeField] public Transform map_spawnzones_parent;
    //[NonSerialized] public Collider[] map_spawnzones;
    [NonSerialized] public map_element_spawn[] map_spawnzones;
    [NonSerialized] public ItemSpawner[] map_item_spawns;
    [NonSerialized] public BouncePad[] map_bouncepads;
    [NonSerialized] public CaptureZone[] map_capturezones;
    [NonSerialized] public Transform[] map_campoints;
    [SerializeField] public GameObject room_game_extended;
    [SerializeField] public GameObject room_spectator_area;
    [SerializeField] public Transform room_spectator_spawn;

    // Unused, but store just to be safe
    [SerializeField] public byte max_players;
    [SerializeField] public byte max_teams;
    [SerializeField] public byte max_players_per_team;


    void Start()
    {

        //map_spawnzones = GetCollidersFromParent(map_spawnzones_parent); // If we have more than 1000 of these, there's a problem
        map_spawnzones = GetSpawnzonesFromParent(transform); // If we have more than 1000 of these, there's a problem
        map_item_spawns = GetItemSpawnerFromParent(transform); // If we have more than 1000 of these, there's a problem
        map_bouncepads = GetBouncePadFromParent(transform); // If we have more than 1000 of these, there's a problem
        map_capturezones = GetCapturezonesFromParent(transform);
        map_campoints = GetCamPointsFromParent(transform);
        //Debug.Log("[" +map_name + "] BOUNCEPADS: " + map_bouncepads.Length);
    }

    public Collider[] GetCollidersFromParent(Transform parent_transform)
    {
        Collider[] array_working = new Collider[1000];
        ushort it_cnt = 0;
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            Collider component = t.GetComponent<Collider>();
            if (t.GetComponent<Collider>() != null)
            {
                array_working[it_cnt] = component;
                if (array_working[it_cnt].GetComponent<Renderer>() != null) { array_working[it_cnt].GetComponent<Renderer>().enabled = false; }
                it_cnt++;
            }
        }
        Collider[] array_condensed = new Collider[it_cnt];
        for (int i = 0; i < it_cnt; i++)
        {
            array_condensed[i] = array_working[i];
        }
        array_working = array_condensed;

        return array_condensed;
    }

    public map_element_spawn[] GetSpawnzonesFromParent(Transform parent_transform)
    {
        map_element_spawn[] array_working = new map_element_spawn[1000];
        ushort it_cnt = 0;
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            map_element_spawn component = t.GetComponent<map_element_spawn>();
            if (t.GetComponent<map_element_spawn>() != null)
            {
                array_working[it_cnt] = component;
                array_working[it_cnt].spawnzone_global_index = it_cnt;
                it_cnt++;
            }
        }
        map_element_spawn[] array_condensed = new map_element_spawn[it_cnt];
        for (int i = 0; i < it_cnt; i++)
        {
            array_condensed[i] = array_working[i];
        }
        array_working = array_condensed;

        return array_condensed;
    }

    public ItemSpawner[] GetItemSpawnerFromParent(Transform parent_transform)
    {
        ItemSpawner[] array_working = new ItemSpawner[1000];
        ushort it_cnt = 0;
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            ItemSpawner component = t.GetComponent<ItemSpawner>();
            if (t.GetComponent<ItemSpawner>() != null)
            {
                array_working[it_cnt] = component;
                array_working[it_cnt].item_spawn_global_index = it_cnt;
                it_cnt++;
            }
        }
        ItemSpawner[] array_condensed = new ItemSpawner[it_cnt];
        for (int i = 0; i < it_cnt; i++)
        {
            array_condensed[i] = array_working[i];
        }
        array_working = array_condensed;

        return array_condensed;
    }

    public BouncePad[] GetBouncePadFromParent(Transform parent_transform)
    {
        BouncePad[] array_working = new BouncePad[1000];
        ushort it_cnt = 0;
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            //Debug.Log("["+ parent_transform.name + "] ( " + it_cnt + ") Search transform: " + t.name);
            BouncePad component = t.GetComponent<BouncePad>();
            if (t.GetComponent<BouncePad>() != null)
            {
                array_working[it_cnt] = component;
                array_working[it_cnt].bouncepad_global_index = it_cnt;
                //array_working[it_cnt].gameObject.SetActive(false);
                //Debug.Log("[" + parent_transform.name + "] ( " + it_cnt + ") BOUNCEPAD: " + component.name);
                it_cnt++;
            }
        }
        BouncePad[] array_condensed = new BouncePad[it_cnt];
        for (int i = 0; i < it_cnt; i++)
        {
            array_condensed[i] = array_working[i];
        }
        array_working = array_condensed;

        return array_condensed;
    }

    public CaptureZone[] GetCapturezonesFromParent(Transform parent_transform)
    {
        CaptureZone[] array_working = new CaptureZone[1000];
        ushort it_cnt = 0;
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            CaptureZone component = t.GetComponent<CaptureZone>();
            if (t.GetComponent<CaptureZone>() != null)
            {
                array_working[it_cnt] = component;
                array_working[it_cnt].global_index = it_cnt;
                component.gameObject.SetActive(false);
                it_cnt++;
            }
        }
        CaptureZone[] array_condensed = new CaptureZone[it_cnt];
        for (int i = 0; i < it_cnt; i++)
        {
            array_condensed[i] = array_working[i];
        }
        array_working = array_condensed;

        return array_condensed;
    }

    public Transform[] GetCamPointsFromParent(Transform parent_transform)
    {
        Transform[] array_working = new Transform[1000];
        ushort it_cnt = 0;
        Transform[] AllChildren = parent_transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in AllChildren)
        {
            if (t != null && t.name.Contains("CamPoint"))
            {
                t.gameObject.SetActive(false);
                array_working[it_cnt] = t;
                it_cnt++;
            }
        }
        Transform[] array_condensed = new Transform[it_cnt];
        for (int i = 0; i < it_cnt; i++)
        {
            array_condensed[i] = array_working[i];
        }
        array_working = array_condensed;

        return array_condensed;
    }

}

