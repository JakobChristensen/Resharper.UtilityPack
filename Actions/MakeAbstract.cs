namespace UtilityPack.Actions
{
  using System;
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.ProjectModel;
  using JetBrains.ReSharper.Feature.Services.Bulbs;
  using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
  using JetBrains.ReSharper.Intentions.Extensibility;
  using JetBrains.ReSharper.Psi.CSharp.Tree;
  using JetBrains.TextControl;
  using JetBrains.Util;

  /// <summary>
  /// Class MakeAbstract
  /// </summary>
  [ContextAction(Name = "Make virtual member abstract [Utility Pack]", Description = "Converts a virtual method to an abstract method.", Group = "C#")]
  public class MakeAbstract : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// 
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MakeAbstract"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public MakeAbstract(ICSharpContextActionDataProvider provider)
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
        return "Make abstract [Utility Pack]";
      }
    }

    #endregion

    #region Public Methods

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

      var function = model.Method;
      if (function != null)
      {
        function.SetAbstract(true);
        function.SetVirtual(false);
        function.SetBody(null);
      }

      var property = model.Property;
      if (property != null)
      {
        property.SetAbstract(true);
        property.SetVirtual(false);

        foreach (var accessor in property.AccessorDeclarations)
        {
          accessor.SetBody(null);
        }
      }

      model.Class.SetAbstract(true);

      return null;
    }

    /// <summary>Gets the model.</summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var selectedElement = this.provider.SelectedElement;
      if (selectedElement == null)
      {
        return null;
      }

      var text = selectedElement.GetText();
      if (text != "virtual")
      {
        return null;
      }

      var function = this.provider.GetSelectedElement<IMethodDeclaration>(true, true);
      if (function != null && !function.IsVirtual)
      {
        return null;
      }

      var property = this.provider.GetSelectedElement<IPropertyDeclaration>(true, true);
      if (property != null && !property.IsVirtual)
      {
        return null;
      }

      if (function == null && property == null)
      {
        return null;
      }

      var @class = this.provider.GetSelectedElement<IClassDeclaration>(true, true);
      if (@class == null)
      {
        return null;
      }

      return new Model
      {
        Class = @class,
        Method = function,
        Property = property
      };
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the class.
      /// </summary>
      /// <value>The class.</value>
      [NotNull]
      public IClassDeclaration Class
      {
        get;
        set;
      }

      /// <summary>
      /// Gets or sets the function.
      /// </summary>
      /// <value>The function.</value>
      [CanBeNull]
      public IMethodDeclaration Method
      {
        get;
        set;
      }

      /// <summary>
      /// Gets or sets the property.
      /// </summary>
      /// <value>The property.</value>
      [CanBeNull]
      public IPropertyDeclaration Property
      {
        get;
        set;
      }

      #endregion
    }
  }
}
