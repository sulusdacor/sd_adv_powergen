/*
 * Created by SharpDevelop.
 * User: sulusdacor
 * Date: 17.11.2016
 * Time: 10:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
//using Verse.AI;          // Needed when you do something with the AI
//using Verse.Sound;       // Needed when you do something with Sound
//using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
//using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains

namespace sd_adv_powergen
{
	[StaticConstructorOnStartup]
	public class sd_adv_powergen_CompAdvPowerPlantSolar : CompPowerPlant
	{
		private const float sd_adv_powergen_FullSunPower = 3400f ;

		private const float sd_adv_powergen_NightPower = 0f;

		private static readonly Vector2 sd_adv_powergen_BarSize = new Vector2(2.3f, 0.14f);

		private static readonly Material sd_adv_powergen_PowerPlantSolarBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));

		private static readonly Material sd_adv_powergen_PowerPlantSolarBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

		protected override float DesiredPowerOutput
		{
			get
			{
				return Mathf.Lerp(0f, 3400f, this.parent.Map.skyManager.CurSkyGlow) * this.RoofedPowerOutputFactor;
			}
		}

		private float RoofedPowerOutputFactor
		{
			get
			{
				int num = 0;
				int num2 = 0;
				foreach (IntVec3 current in this.parent.OccupiedRect())
				{
					num++;
					if (this.parent.Map.roofGrid.Roofed(current))
					{
						num2++;
					}
				}
				return (float)(num - num2) / (float)num;
			}
		}

		public override void PostDraw()
		{
			base.PostDraw();
			GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
			r.center = this.parent.DrawPos + Vector3.up * 0.1f;
			r.size = sd_adv_powergen_CompAdvPowerPlantSolar.sd_adv_powergen_BarSize;
			r.fillPercent = base.PowerOutput / 3400f;
			r.filledMat = sd_adv_powergen_CompAdvPowerPlantSolar.sd_adv_powergen_PowerPlantSolarBarFilledMat;
			r.unfilledMat = sd_adv_powergen_CompAdvPowerPlantSolar.sd_adv_powergen_PowerPlantSolarBarUnfilledMat;
			r.margin = 0.15f;
			Rot4 rotation = this.parent.Rotation;
			rotation.Rotate(RotationDirection.Clockwise);
			r.rotation = rotation;
			GenDraw.DrawFillableBar(r);
		}
	}
	
	public class PlaceWorker_AdvWatermillGenerator : PlaceWorker
	{
		private static List<Thing> advwaterMills = new List<Thing>();

		public AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
		{
			foreach (IntVec3 current in CompPowerPlantWater.GroundCells(loc, rot))
			{
				if (!map.terrainGrid.TerrainAt(current).affordances.Contains(TerrainAffordanceDefOf.Heavy))
				{
					AcceptanceReport result = new AcceptanceReport("TerrainCannotSupport_TerrainAffordance".Translate(checkingDef, TerrainAffordanceDefOf.Heavy));
					return result;
				}
			}
			if (!this.WaterCellsPresent(loc, rot, map))
			{
				return new AcceptanceReport("MustBeOnMovingWater".Translate());
			}
			return true;
		}

		private bool WaterCellsPresent(IntVec3 loc, Rot4 rot, Map map)
		{
			foreach (IntVec3 current in CompPowerPlantWater.WaterCells(loc, rot))
			{
				if (!map.terrainGrid.TerrainAt(current).affordances.Contains(TerrainAffordanceDefOf.MovingFluid))
				{
					return false;
				}
			}
			return true;
		}

		public void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol)
		{
			GenDraw.DrawFieldEdges(CompPowerPlantWater.GroundCells(loc, rot).ToList<IntVec3>(), Color.white);
			Color color = (!this.WaterCellsPresent(loc, rot, Find.CurrentMap)) ? Designator_Place.CannotPlaceColor.ToOpaque() : Designator_Place.CanPlaceColor.ToOpaque();
			GenDraw.DrawFieldEdges(CompPowerPlantWater.WaterCells(loc, rot).ToList<IntVec3>(), color);
			bool flag = false;
			CellRect cellRect = CompPowerPlantWater.WaterUseRect(loc, rot);
			PlaceWorker_AdvWatermillGenerator.advwaterMills.AddRange(Find.CurrentMap.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.sd_adv_powergen_WatermillGenerator).Cast<Thing>());
			PlaceWorker_AdvWatermillGenerator.advwaterMills.AddRange(from t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint)
			where t.def.entityDefToBuild == ThingDefOf.sd_adv_powergen_WatermillGenerator
			select t);
			PlaceWorker_AdvWatermillGenerator.advwaterMills.AddRange(from t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame)
			where t.def.entityDefToBuild == ThingDefOf.sd_adv_powergen_WatermillGenerator
			select t);
			foreach (Thing current in PlaceWorker_AdvWatermillGenerator.advwaterMills)
			{
				GenDraw.DrawFieldEdges(CompPowerPlantWater.WaterUseCells(current.Position, current.Rotation).ToList<IntVec3>(), new Color(0.2f, 0.2f, 1f));
				if (cellRect.Overlaps(CompPowerPlantWater.WaterUseRect(current.Position, current.Rotation)))
				{
					flag = true;
				}
			}
			PlaceWorker_AdvWatermillGenerator.advwaterMills.Clear();
			Color color2 = (!flag) ? Designator_Place.CanPlaceColor.ToOpaque() : new Color(1f, 0.6f, 0f);
			if (!flag || Time.realtimeSinceStartup % 0.4f < 0.2f)
			{
				GenDraw.DrawFieldEdges(CompPowerPlantWater.WaterUseCells(loc, rot).ToList<IntVec3>(), color2);
			}
		}

		[DebuggerHidden]
		public override IEnumerable<TerrainAffordanceDef> DisplayAffordances()
		{
			yield return TerrainAffordanceDefOf.Heavy;
			yield return TerrainAffordanceDefOf.MovingFluid;
		}
	}
	
	[DefOf]
	public static class ThingDefOf
	{
		public static ThingDef sd_adv_powergen_WatermillGenerator;	
		
	}
}