namespace Cephalon.Abstractions.Behaviors;

/// <summary>
/// Assembly-level marker indicating that the assembly contains auto-discovered
/// behavior types. When present, the engine uses the source-generated registration
/// class instead of runtime reflection scanning, resulting in zero-reflection startup.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is automatically emitted by the <c>Cephalon.Behaviors.SourceGen</c>
/// source generator when it discovers one or more <c>[AppBehavior]</c> classes in the assembly.
/// You do not need to add it manually.
/// </para>
/// <para>
/// The <see cref="RegistrationType"/> property points to the generated class that provides
/// compile-time registration and topology descriptors, enabling the engine to skip
/// the expensive <c>DefinedTypes</c> / <c>GetCustomAttribute</c> reflection scan.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class ContainsBehaviorsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ContainsBehaviorsAttribute"/>
    /// pointing to the generated registration class.
    /// </summary>
    /// <param name="registrationType">
    /// The generated type that contains static <c>Register</c> and <c>GetTopologyDescriptors</c> methods.
    /// </param>
    public ContainsBehaviorsAttribute(Type registrationType)
    {
        RegistrationType = registrationType ?? throw new ArgumentNullException(nameof(registrationType));
    }

    /// <summary>
    /// Gets the generated registration class type emitted by the source generator.
    /// </summary>
    public Type RegistrationType { get; }
}
