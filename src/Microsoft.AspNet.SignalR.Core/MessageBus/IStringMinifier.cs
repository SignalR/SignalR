namespace Microsoft.AspNet.SignalR
{
    public interface IStringMinifier
    {
        /// <summary>
        /// Minifies a string in a way that can be reversed by this instance of <see cref="IStringMinifier"/>.
        /// </summary>
        /// <param name="fullString">The string to be minified</param>
        /// <returns>A minified representation of the <see cref="fullString"/> without the following characters:,|\</returns>
        string Minify(string fullString);

        /// <summary>
        /// Reverses a <see cref="Minify"/> call that was executed at least once previously on this instance of
        /// <see cref="IStringMinifier"/> without any subsequent calls to <see cref="RemoveUnminified"/> sharing the
        /// same argument as the <see cref="Minify"/> call that returned <see cref="minifiedString"/>.
        /// </summary>
        /// <param name="minifiedString">
        /// A minified string that was returned by a previous call to <see cref="Minify"/>.
        /// </param>
        /// <returns>
        /// The argument of all previous calls to <see cref="Minify"/> that returned <see cref="minifiedString"/>.
        /// If every call to <see cref="Minify"/> on this instance of <see cref="IStringMinifier"/> has never
        /// returned <see cref="minifiedString"/> or if the most recent call to <see cref="Minify"/> that did
        /// return <see cref="minifiedString"/> was followed by a call to <see cref="RemoveUnminified"/> sharing 
        /// the same argument, <see cref="Unminify"/> may return null but must not throw.
        /// </returns>
        string Unminify(string minifiedString);

        /// <summary>
        /// A call to this function indicates that any future attempt to unminify strings that were previously minified
        /// from <see cref="fullString"/> may be met with a null return value. This provides an opportunity clean up
        /// any internal data structures that reference <see cref="fullString"/>.
        /// </summary>
        /// <param name="fullString">The string that may have previously have been minified.</param>
        void RemoveUnminified(string fullString);
    }
}
