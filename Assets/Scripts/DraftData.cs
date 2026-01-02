using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DraftData
{
    // Metadane
    public string author;
    public string name;
    public DateTime date;
    public string version;

    // Dane Tekstury
    public byte[] mapTexture;
    public byte[] drawTexture;
}
