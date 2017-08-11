// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using CivOne.Advances;
using CivOne.Graphics;
using CivOne.Interfaces;

namespace CivOne.Buildings
{
	public interface IBuilding : ICivilopedia, IProduction
	{
		byte Id { get; }
		IAdvance RequiredTech { get; }
		byte Maintenance { get; }
		Picture SmallIcon { get; }
		short SellPrice { get; }
	}
}