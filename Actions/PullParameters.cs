namespace UtilityPack.Actions
{
  using System;
  using System.Collections.Generic;
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.ProjectModel;
  using JetBrains.ReSharper.Feature.Services.Bulbs;
  using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
  using JetBrains.ReSharper.Intentions.Extensibility;
  using JetBrains.ReSharper.Psi;
  using JetBrains.ReSharper.Psi.CSharp.Tree;
  using JetBrains.TextControl;
  using JetBrains.Util;

  /// <summary>
  /// Defines the PullParameters class.
  /// </summary>
  [ContextAction(Name = "Pull parameters [UtilityPack]", Description = "Pulls the containing methods parameters to this method call.", Group = "C#")]
  public class PullParameters : ContextActionBase
  {
    #region Fields

    /// <summary>
    /// The provider
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PullParameters"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public PullParameters(ICSharpContextActionDataProvider provider)
    {
      this.provider = provider;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Popup menu item text
    /// </summary>
    /// <value>The text.</value>
    public override string Text
    {
      get
      {
        return "Pull parameters [Utility Pack]";
      }
    }

    #endregion

    #region Public Methods and Operators

    /// <summary>
    /// Determines whether the specified cache is available.
    /// </summary>
    /// <param name="cache">The cache.</param>
    /// <returns><c>true</c> if the specified cache is available; otherwise, <c>false</c>.</returns>
    public override bool IsAvailable(IUserDataHolder cache)
    {
      return this.GetModel() != null;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Executes QuickFix or ContextAction. Returns post-execute method.
    /// </summary>
    /// <param name="solution">The solution.</param>
    /// <param name="progress">The progress.</param>
    /// <returns>Action to execute after document and PSI transaction finish. Use to open TextControls, navigate caret, etc.</returns>
    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
      var model = this.GetModel();
      if (model == null)
      {
        return null;
      }

      foreach (var parameter in model.Parameters.Reverse())
      {
        var expression = this.provider.ElementFactory.CreateExpression(parameter.ShortName);

        var argument = this.provider.ElementFactory.CreateArgument(ParameterKind.VALUE, expression);

        model.InvocationExpression.AddArgumentAfter(argument, null);
      }

      return null;
    }

    /// <summary>
    /// Gets the model.
    /// </summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var invocationExpression = this.provider.GetSelectedElement<IInvocationExpression>(true, true);
      if (invocationExpression == null)
      {
        return null;
      }

      var arguments = invocationExpression.Arguments;
      if (arguments.Any())
      {
        return null;
      }

      var method = this.provider.GetSelectedElement<IMethodDeclaration>(true, true);
      if (method == null)
      {
        return null;
      }

      if (method.DeclaredElement == null)
      {
        return null;
      }

      var parameters = method.DeclaredElement.Parameters;
      if (!parameters.Any())
      {
        return null;
      }

      var reference = invocationExpression.InvokedExpression as IReferenceExpression;
      if (reference == null)
      {
        return null;
      }

      var resolveResult = reference.Reference.Resolve();
      var declaredElement = resolveResult.DeclaredElement;
      if (declaredElement == null)
      {
        return null;
      }

      var parametersOwner = declaredElement as IParametersOwner;
      if (parametersOwner == null)
      {
        return null;
      }

      var invokedParameters = parametersOwner.Parameters;
      if (!invokedParameters.Any())
      {
        return null;
      }

      return new Model
      {
        InvocationExpression = invocationExpression,
        ContainingMethod = method,
        Parameters = invokedParameters
      };
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the method.
      /// </summary>
      /// <value>The method.</value>
      [NotNull]
      public IMethodDeclaration ContainingMethod { get; set; }

      /// <summary>
      /// Gets or sets the invocation expression.
      /// </summary>
      /// <value>The invocation expression.</value>
      [NotNull]
      public IInvocationExpression InvocationExpression { get; set; }

      /// <summary>
      /// Gets or sets the parameters.
      /// </summary>
      /// <value>The parameters.</value>
      public IList<IParameter> Parameters { get; set; }

      #endregion
    }
  }
}