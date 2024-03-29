using Arch.Core;

namespace Systems
{
	/// <summary>
	///     The <see cref="SystemBase{T}"/> class
	///     is a rudimentary basis for all systems with some important methods and properties.
	/// </summary>
	/// <typeparam name="T">The generic type passed to the <see cref="Update"/> method.</typeparam>
	/// <remarks>
	///     Initializes a new instance of the <see cref="SystemBase{T}"/> class.
	/// </remarks>
	/// <param name="world">Its <see cref="World"/>.</param>
	public abstract class SystemBase<T>(World world)
	{

		/// <summary>
		///     The <see cref="World"/> for which this system works and must access.
		/// </summary>
		public World World { get; private set; } = world;

		/// <summary>
		///     Should be called within the update loop to update this system and executes its logic.
		/// </summary>
		/// <param name="state">A external state being passed to this method to be used.</param>
		public abstract void Update(in T state);
	}
}
