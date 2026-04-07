using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Sample.Showcase.Modules;

/// <summary>
/// Module that contributes showcase authorization policies demonstrating RBAC with three roles:
/// viewer (read-only), customer (read + cart + orders), and admin (full access).
/// </summary>
public sealed class ShowcaseAuthorizationPolicyModule : ModuleBase, IAuthorizationPolicyContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "showcase.authorization",
        displayName: "Showcase Authorization",
        description: "Contributes RBAC authorization policies for the showcase sample with viewer, customer, and admin roles.",
        tags: ["showcase", "authorization", "rbac"],
        version: "1.0.0");

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <summary>
    /// Registers the showcase authorization policies in the policy registry.
    /// </summary>
    /// <param name="policies">The authorization policy registry.</param>
    public void RegisterPolicies(IAuthorizationPolicyRegistry policies)
    {
        policies.Add(new AuthorizationPolicyDescriptor(
            id: "showcase.viewer",
            displayName: "Viewer",
            description: "Read-only access to the catalog and order status. Cannot modify cart or place orders.",
            modes: [AuthorizationMode.Rbac],
            tags: ["showcase", "viewer"],
            metadata: new Dictionary<string, string>
            {
                ["roles"] = "viewer",
                ["allowedBehaviors"] = "catalog.get-product,catalog.list-products,orders.get-status,shipping.track"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: "showcase.customer",
            displayName: "Customer",
            description: "Full customer access: browse catalog, manage cart, place and track orders.",
            modes: [AuthorizationMode.Rbac],
            tags: ["showcase", "customer"],
            metadata: new Dictionary<string, string>
            {
                ["roles"] = "customer",
                ["allowedBehaviors"] = "catalog.*,cart.*,orders.place,orders.cancel,orders.get-status,shipping.track"
            }));

        policies.Add(new AuthorizationPolicyDescriptor(
            id: "showcase.admin",
            displayName: "Admin",
            description: "Full administrative access to all showcase behaviors including inventory and shipping management.",
            modes: [AuthorizationMode.Rbac],
            tags: ["showcase", "admin"],
            metadata: new Dictionary<string, string>
            {
                ["roles"] = "admin",
                ["allowedBehaviors"] = "*"
            }));
    }
}
