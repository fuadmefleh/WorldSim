using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace WorldSimAPI
{
    
	public enum SettlementType
	{
		HunterGather,

		Hamlet,
		Village, // 150 to 500

		Township,
		Town

	}

    public enum BiomeType
    {
        Desert,
        Savanna,
        TropicalRainforest,
        Grassland,
        Woodland,
        SeasonalForest,
        TemperateRainforest,
        BorealForest,
        Tundra,
        Ice
    }

    public enum Direction
	{
		North = 0,
		NorthEast = 1,
		East = 2,
		SouthEast =3,
		South =4,
		SouthWest =5,
		West =6,
		NorthWest =7
	}

	public enum HeightType
	{
		[Description("Deep Water")]
		DeepWater = 1,

		[Description("Shallow Water")]
		ShallowWater = 2,

		[Description("Shore")]
		Shore = 3,

		[Description("Sand")]
		Sand = 4,

		[Description("Grass")]
		Grass = 5,

		[Description("Forest")]
		Forest = 6,

		[Description("Rock")]
		Rock = 7,

		[Description("Snow")]
		Snow = 8,

		[Description("River")]
		River = 9,

		[Description("Dirt")]
		Dirt = 10,

		[Description("Scrublands")]
		Scrublands = 11,

		[Description("Highlands")]
		Highlands = 12,

		[Description("Hills")]
		Hills = 13,

		[Description("Mountain")]
		Mountain = 14
	}

	public enum HeatType
	{
		[Description("Coldest")]
		Coldest = 0,
		[Description("Colder")]
		Colder = 1,
		[Description("Cold")]
		Cold = 2,
		[Description("Warm")]
		Warm = 3,
		[Description("Warmer")]
		Warmer = 4,
		[Description("Warmest")]
		Warmest = 5
	}

	public enum MoistureType
	{
		[Description("Wettest")]
		Wettest = 5,
		[Description("Wetter")]
		Wetter = 4,
		[Description("Wet")]
		Wet = 3,
		[Description("Dry")]
		Dry = 2,
		[Description("Dryer")]
		Dryer = 1,
		[Description("Dryest")]
		Dryest = 0
	}

}
