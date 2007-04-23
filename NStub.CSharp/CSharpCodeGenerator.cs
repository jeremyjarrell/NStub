using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;
using NStub.Core;
using NStub.CSharp;
using NUnit.Framework;

namespace NStub.CSharp
{
	/// <summary>
	/// The CSharpCodeGenerator is responsible for the generation of the individual
	/// class files which will make up the actual test project.  For information
	/// regarding the generation of the project file, see 
	/// <see cref="CSharpProjectGenerator">CSharpProjectGenerator</see>.
	/// </summary>
	public class CSharpCodeGenerator : Core.ICodeGenerator
	{
		#region Fields (Private)

		private CodeNamespace _codeNamespace;
		private string _outputDirectory;

		#endregion Fields (Private)

		#region Constructor (Public)

		/// <summary>
		/// Initializes a new instance of the <see cref="CSharpCodeGenerator"/> class
		/// based the given CodeNamespace which will output to the given directory.
		/// </summary>
		/// <param name="codeNamespace">The code namespace.</param>
		/// <param name="outputDirectory">The output directory.</param>
		/// <exception cref="System.ArgumentNullException">codeNamepsace or
		/// outputDirectory is null.</exception>
		/// <exception cref="System.ArgumentException">outputDirectory is an
		/// empty string.</exception>
		/// <exception cref="DirectoryNotFoundException">outputDirectory
		/// cannot be found.</exception>
		public CSharpCodeGenerator(CodeNamespace codeNamespace, 
			string outputDirectory)
		{
			#region Validation

			// Null arguments will not be accepted
			if (codeNamespace == null)
			{
				throw new ArgumentNullException("codeNamespace",
					Exceptions.ParameterCannotBeNull);
			}
			if (outputDirectory == null)
			{
				throw new ArgumentNullException("outputDirectory",
					Exceptions.ParameterCannotBeNull);
			}
			// Ensure that the output directory is not empty
			if (outputDirectory.Length == 0)
			{
				throw new ArgumentException(Exceptions.StringCannotBeEmpty,
					"outputDirectory");
			}
			// Ensure that the output directory is valid
			if (!(Directory.Exists(outputDirectory)))
			{
				throw new DirectoryNotFoundException(Exceptions.DirectoryCannotBeFound);
			}

			#endregion Validation

			_codeNamespace = codeNamespace;
			_outputDirectory = outputDirectory;
		}

		#endregion Constructor (Public)

		#region Properties (Public)

		/// <summary>
		/// Gets or sets the directory the new sources files will be output to.
		/// </summary>
		/// <value>The directory the new source files will be output to.</value>
		public string OutputDirectory
		{
			get
			{
				return _outputDirectory;
			}
			set
			{
				_outputDirectory = value;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="System.CodeDom.CodeNamespace"/> object 
		/// the generator is currently working from.
		/// </summary>
		/// <value>The <see cref="System.CodeDom.CodeNamespace"/> object the 
		/// generator is currently working from.</value>
		public CodeNamespace CodeNamespace
		{
			get
			{
				return _codeNamespace;
			}
			set
			{
				_codeNamespace = value;
			}
		}
		
		#endregion Properties (Public)

		#region Methods (Public)

		/// <summary>
		/// This methods actually performs the code generation for the
		/// file current <see cref="System.CodeDom.CodeNamespace">CodeNamespace</see>. 
		/// All classes within the namespace will have exactly one file generated for them.  
		/// </summary>
		public void GenerateCode()
		{
			// We want to write a separate file for each type
			foreach (CodeTypeDeclaration codeTypeDeclaration in _codeNamespace.Types)
			{
				// Create a namespace for the Type in order to put it in scope
				CodeNamespace codeNamespace = 
					new CodeNamespace((_codeNamespace.Name));

				// Create our test type
				codeTypeDeclaration.Name = 
					Utility.GetUnqualifiedTypeName(codeTypeDeclaration.Name) + "Test";
				codeTypeDeclaration.IsPartial = true;

				// Give it a default public constructor
				CodeConstructor codeConstructor = new CodeConstructor();
				codeConstructor.Attributes = MemberAttributes.Public;
				codeTypeDeclaration.Members.Add(codeConstructor);

				// Set out member names correctly
				foreach (CodeTypeMember typeMember in codeTypeDeclaration.Members)
				{
					if (typeMember is CodeMemberMethod)
					{					
						CreateStubForCodeMemberMethod(typeMember as CodeMemberMethod);
					}
					else
					{
						CreateStubForCodeTypeMember(typeMember);
					}
				}
				codeNamespace.Types.Add(codeTypeDeclaration);
				WriteClassFile(codeTypeDeclaration.Name, codeNamespace);
			}
		}

		#endregion Methods (Public)

		#region Helper Methods (Private)

		/// <summary>
		/// Creates the stub for code type member.  In this case we only append
		/// the user's selected test name to the method.
		/// </summary>
		/// <param name="codeTypeMember">The code type member to be generated.</param>
		private static void CreateStubForCodeTypeMember(CodeTypeMember codeTypeMember)
		{
			codeTypeMember.Name = codeTypeMember.Name + "Test";
		}

		/// <summary>
		/// Creates the stub for the code member method.  This method actually
		/// implements the method body for the test method.
		/// </summary>
		/// <param name="codeMemberMethod">The code member method.</param>
		private static void CreateStubForCodeMemberMethod(CodeMemberMethod codeMemberMethod)
		{
			CreateStubForCodeTypeMember(codeMemberMethod);
			codeMemberMethod.CustomAttributes.Add(
				new CodeAttributeDeclaration(
					new CodeTypeReference(typeof(TestAttribute))));
			codeMemberMethod.Statements.Add(
				new CodeCommentStatement("TODO: Implement unit test for " + 
				codeMemberMethod.Name));
			codeMemberMethod.Statements.Add(
				new CodeThrowExceptionStatement(
					new CodeTypeReferenceExpression(typeof(NotImplementedException))));
		}

		/// <summary>
		/// Writes the class file.  This method actually creates the physical
		/// class file and populates it accordingly.
		/// </summary>
		/// <param name="className">Name of the class file to be written.</param>
		/// <param name="codeNamespace">The CodeNamespace which represents the
		/// file to be written.</param>
		private void WriteClassFile(string className, CodeNamespace codeNamespace)
		{
			CSharpCodeProvider cSharpCodeProvider = new CSharpCodeProvider();
			string sourceFile = _outputDirectory + Path.DirectorySeparatorChar +
				className + "." + cSharpCodeProvider.FileExtension;
			sourceFile = Utility.ScrubPathOfIllegalCharacters(sourceFile);
			IndentedTextWriter indentedTextWriter =
				new IndentedTextWriter(new StreamWriter(sourceFile, false), "  ");
			CodeGeneratorOptions codeGenerationOptions = new CodeGeneratorOptions();
			codeGenerationOptions.BracingStyle = "C";
			cSharpCodeProvider.GenerateCodeFromNamespace(codeNamespace, indentedTextWriter, 
				codeGenerationOptions);
			indentedTextWriter.Flush();
			indentedTextWriter.Close();
		}

		#endregion Helper Methods (Private)
	}
}
