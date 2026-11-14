using System;
using System.Collections.Generic;

namespace OutpostGale.Sim
{
    // Shapes of the bundled NASA data (Assets/StreamingAssets/data/*.json).
    // Written by tools/fetch_data; never edited by hand.

    [Serializable]
    public class DonkiStorm
    {
        public string id;          // DONKI SEP activity id
        public string date;        // yyyy-mm-dd
        public string flareClass;  // linked flare class, may be empty
        public string flareId;
        public string cmeId;
        public bool marsRelated;
        public string instruments;
        public string link;
    }

    [Serializable]
    public class DonkiFlare
    {
        public string id;
        public string date;
        public string flareClass;
        public string link;
    }

    [Serializable]
    public class DonkiSeason
    {
        public int year;
        public int stormCount;
        public string kind;        // calm | moderate | stormy
        public string strongestFlare;
        public DonkiStorm[] storms;
        public DonkiFlare[] flares;
        public string source;
        public string retrieved_on;
    }

    [Serializable]
    public class DonkiArchive
    {
        public DonkiSeason[] seasons;
    }

    [Serializable]
    public class FactCard
    {
        public string id;
        public string title;
        public string text;
        public string meter;
        public string studyTitle;
        public string url;
        public string source;
        public string retrieved_on;
    }

    [Serializable]
    public class FactArchive
    {
        public FactCard[] facts;
    }

    [Serializable]
    public class RadiationInfo
    {
        public string site;
        public float baselineDoseEquivalent_mSvPerSol;
        public float baselineAbsorbedDose_mGyPerSol;
        public float chestXray_mSv;
        public string citation;
        public string note;
        public string source;
        public string retrieved_on;
    }

    // Everything the sim needs from the archive, bundled for a new mission.
    public class GameData
    {
        public DonkiArchive donki;
        public FactArchive osdr;
        public RadiationInfo radiation;
        public StoryScript story;
        public List<StaticFact> staticFacts = new List<StaticFact>();
    }

    [Serializable]
    public class StaticFact
    {
        public string id;
        public string title;
        public string text;
        public string source;
        public string trigger;     // when it can unlock: night | storm | dust | build:<module> | ending
    }

    [Serializable]
    public class StaticFactList
    {
        public StaticFact[] facts;
    }
}
