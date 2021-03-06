// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Civilizations;
using CivOne.Concepts;
using CivOne.Governments;
using CivOne.Graphics.Sprites;
using CivOne.Leaders;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Wonders;

namespace CivOne
{
	internal static class Reflect
	{
		private static void Log(string text, params object[] parameters) => RuntimeHandler.Runtime.Log(text, parameters);

		private static Plugin[] _plugins;
		private static void LoadPlugins()
		{
			if (_plugins != null) return;
			_plugins = Directory.GetFiles(Settings.Instance.PluginsDirectory, "*.dll").Select(x => Plugin.Load(x)).Where(x => x != null).ToArray();

			string[] disabledPlugins = Settings.Instance.DisabledPlugins.ToArray();
			if (_plugins.Any(x => !disabledPlugins.Contains(x.Filename)))
			{
				Settings.Instance.DisabledPlugins = _plugins.Where(x => !x.Enabled).Select(x => x.Filename).ToArray();
			}
		}

		internal static void LoadPlugin(string filename)
		{
			if (!Plugin.Validate(filename)) return;

			List<Plugin> plugins = new List<Plugin>(_plugins ?? new Plugin[0]);

			Plugin plugin = Plugin.Load(filename);
			plugin.Enabled = true;

			plugins.RemoveAll(x => x.Filename == Path.GetFileName(filename));
			plugins.Add(plugin);

			_plugins = plugins.ToArray();

			ApplyPlugins();
		}

		private static IEnumerable<Assembly> GetAssemblies
		{
			get
			{
				yield return typeof(Reflect).GetTypeInfo().Assembly;
			}
		}
		
		private static IEnumerable<T> GetTypes<T>()
		{
			foreach (Assembly asm in GetAssemblies)
			foreach (Type type in asm.GetTypes().Where(t => typeof(T).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()) && t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract))
			{
				yield return (T)Activator.CreateInstance(type);
			}

			foreach (Assembly asm in GetAssemblies)
			foreach (Type type in asm.GetTypes().Where(t => (t is T) && t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract))
			{
				yield return (T)Activator.CreateInstance(type);
			}
		}
		
		internal static IEnumerable<IAdvance> GetAdvances() => GetTypes<IAdvance>().OrderBy(x => x.Id);

		internal static IEnumerable<ICivilization> GetCivilizations() => GetTypes<ICivilization>().OrderBy(x => (int)x.Id);
		
		internal static IEnumerable<IGovernment> GetGovernments() => GetTypes<IGovernment>().OrderBy(x => x.Id);
		
		internal static IEnumerable<IUnit> GetUnits() => GetTypes<IUnit>().OrderBy(x => (int)x.Type);
		
		internal static IEnumerable<IBuilding> GetBuildings() => GetTypes<IBuilding>().OrderBy(x => x.Id);
		
		internal static IEnumerable<IWonder> GetWonders() => GetTypes<IWonder>().OrderBy(x => x.Id);

		internal static IEnumerable<IProduction> GetProduction()
		{
			foreach (IProduction production in GetUnits())
				yield return production;
			foreach (IProduction production in GetBuildings())
				yield return production;
			foreach (IProduction production in GetWonders())
				yield return production;
		}
		
		internal static IEnumerable<IConcept> GetConcepts() => GetTypes<IConcept>();
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaAll()
		{
			List<string> articles = new List<string>();
			foreach (ICivilopedia article in GetTypes<ICivilopedia>().OrderBy(a => (a is IConcept) ? 1 : 0))
			{
				if (articles.Contains(article.Name)) continue;
				articles.Add(article.Name);
				yield return article;
			}
		}
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaAdvances() => GetTypes<IAdvance>();
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaCityImprovements()
		{
			foreach (ICivilopedia civilopedia in GetTypes<IBuilding>())
				yield return civilopedia;
			foreach (ICivilopedia civilopedia in GetTypes<IWonder>())
				yield return civilopedia;
		}
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaUnits() => GetTypes<IUnit>();
		
		internal static IEnumerable<ICivilopedia> GetCivilopediaTerrainTypes() => GetTypes<ITile>();

		internal static void ApplyPlugins()
		{
			BaseCivilization.LoadModifications();
			BaseLeader.LoadModifications();
			BaseUnit.LoadModifications();
		}

		internal static IEnumerable<Plugin> Plugins()
		{
			if (_plugins == null)
			{
				LoadPlugins();
				ApplyPlugins();
			}
			return _plugins;
		}

		private static IEnumerable<Type> PluginModifications
		{
			get
			{
				if (_plugins == null) yield break;
				foreach (Assembly assembly in _plugins.Where(x => x.Enabled).Select(x => x.Assembly))
				foreach (Type type in assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Contains(typeof(IModification))))
				{
					yield return type;
				}
			}
		}

		private static object[] ParseParameters(params object[] parameters)
		{
			List<object> output = new List<object>();
			foreach (object parameter in parameters)
			{
				switch (parameter)
				{
					case string stringParameter:
						output.Add(stringParameter);
						break;
					case int intParameter:
						output.Add(intParameter);
						break;
					default:
						output.Add(parameter);
						break;
				}
			}
			return output.ToArray();
		}

		internal static IEnumerable<T> GetModifications<T>()
		{
			foreach (Type type in PluginModifications.Where(x => x.IsSubclassOf(typeof(T))))
			{
				yield return (T)Activator.CreateInstance(type);
			}
		}
		
		internal static void PreloadCivilopedia()
		{
			Log("Civilopedia: Preloading articles...");
			foreach (ICivilopedia article in GetCivilopediaAll());
			Log("Civilopedia: Preloading done!");
		}
	}
}