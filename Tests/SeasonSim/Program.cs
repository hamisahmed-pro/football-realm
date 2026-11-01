using System; using System.IO; using FootballMogul.Core;
class P { static void Main() {
  var data = ClubDatabase.LoadFromJson(File.ReadAllText("/home/hatch/workspace/football-mogul-unity/Assets/Data/clubs.json"));
  var leagues = ClubDatabase.BuildLeagues(data);
  var eng = leagues.Find(l => l.Country=="England" && l.Tier==1);
  var season = new Season(eng); var t = new TacticalSetup();
  int hg=0, ag=0, hw=0, d=0, aw=0; long seed=1000;
  foreach (var f in season.Fixtures) { var r = MatchEngine.Simulate(f.Home, f.Away, t, t, seed++); hg+=r.HomeGoals; ag+=r.AwayGoals; if (r.HomeGoals>r.AwayGoals) hw++; else if (r.HomeGoals==r.AwayGoals) d++; else aw++; }
  int n = season.Fixtures.Count;
  Console.WriteLine($"matches={n} avgTotalGoals={(double)(hg+ag)/n:F2} homeGoals/match={(double)hg/n:F2} awayGoals/match={(double)ag/n:F2} homeWin%={(double)hw/n*100:F1} draw%={(double)d/n*100:F1} awayWin%={(double)aw/n*100:F1}");
}}
