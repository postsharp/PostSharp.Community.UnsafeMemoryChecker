using System;
using System.Collections.Generic;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Community.UnsafeMemoryChecker.Weaver
{

    [ExportTask(TaskName = nameof(CheckUnsafeMemoryAccessTask))]
    [TaskDependency("MethodUsageIndexService")]
    [TaskDependency("ImplementationBoundAttributes")]
    [TaskDependency("IndexTypeDefMemberRefs")]
    public sealed class CheckUnsafeMemoryAccessTask : Task, IAdviceRequiringStackStatus
    {
        private const string wrapperTypeName = "PostSharp.Community.UnsafeMemoryChecker.UnsafeMemoryAccess";
        private TypeDefDeclaration wrapperType;
        readonly Dictionary<OpCodeNumber, IMethod> methods = new Dictionary<OpCodeNumber, IMethod>();

        public TypeDefDeclaration GetWrapperType( AssemblyEnvelope assembly, HashSet<string> visitedAssemblies )
        {
            visitedAssemblies.Add( assembly.Name );

            TypeDefDeclaration result = assembly.GetTypeDefinition( wrapperTypeName, BindingOptions.DontThrowException );

            if ( result != null )
                return result;

            foreach ( ModuleDeclaration module in assembly.Modules )
            {
                foreach ( AssemblyRefDeclaration assemblyRef in module.AssemblyRefs )
                {
                    if ( visitedAssemblies.Contains( assemblyRef.Name ) )
                        continue;

                    AssemblyEnvelope assemblyRefEnvelope = assemblyRef.GetAssemblyEnvelope(BindingOptions.DontThrowException);

                    if (assemblyRefEnvelope == null)
                        continue;

                    result = this.GetWrapperType(assemblyRefEnvelope, visitedAssemblies );

                    if ( result != null )
                        return result;
                }
            }

            return null;
        }

        public override bool Execute()
        {
            this.wrapperType = this.GetWrapperType(this.Project.Module.Assembly, new HashSet<string>());
            if (this.wrapperType == null)
            {
                Message.Write(this.Project.Module.Assembly.GetSystemAssembly(), SeverityType.Error, "CUMA001", "You must create a CheckMemoryAccess class with the appropriate source code in your project.");
                return false;
            }

            this.methods.Add(OpCodeNumber.Stind_I, this.wrapperType.Methods.GetOneByName("StoreIntPtr").Translate(this.Project.Module));
            this.methods.Add(OpCodeNumber.Stind_I1, this.wrapperType.Methods.GetOneByName("StoreByte").Translate(this.Project.Module));
            this.methods.Add(OpCodeNumber.Stind_I2, this.wrapperType.Methods.GetOneByName("StoreInt16").Translate(this.Project.Module));
            this.methods.Add(OpCodeNumber.Stind_I4, this.wrapperType.Methods.GetOneByName("StoreInt32").Translate(this.Project.Module));
            this.methods.Add(OpCodeNumber.Stind_I8, this.wrapperType.Methods.GetOneByName("StoreInt64").Translate(this.Project.Module));
            this.methods.Add(OpCodeNumber.Stind_R4, this.wrapperType.Methods.GetOneByName("StoreSingle").Translate(this.Project.Module));
            this.methods.Add(OpCodeNumber.Stind_R8, this.wrapperType.Methods.GetOneByName("StoreDouble").Translate(this.Project.Module));

            Sdk.CodeWeaver.Weaver weaver = new Sdk.CodeWeaver.Weaver( this.Project );
            weaver.AddMethodLevelAdvice( this, null, JoinPointKinds.InsteadOfStoreIndirect, null );
            weaver.Weave();
            return base.Execute();
        }

        int IAdvice.Priority
        {
            get { return 0; }
        }

        public bool RequiresStackStatus( MethodDefDeclaration method )
        {
            return true;
        }

        bool IAdvice.RequiresWeave( WeavingContext context )
        {
            ITypeSignature addressType = context.StackTypeStatus.GetAt( 1 );

            bool weave = context.Method.DeclaringType != this.wrapperType
                         && this.methods.ContainsKey( context.JoinPoint.Instruction.OpCodeNumber )
                         && !((PointerTypeSignature) addressType.GetNakedType()).IsManaged;

            Console.WriteLine( "method={0}, instruction={1}, address={2}, weave={3}", context.Method, context.JoinPoint.Instruction, addressType, weave );

            return weave;
        }

        void IAdvice.Weave( WeavingContext context, InstructionBlock block )
        {
            Console.WriteLine("Weave");
            IMethod method = this.methods[context.JoinPoint.Instruction.OpCodeNumber];

            InstructionSequence sequence = block.AddInstructionSequence(null, NodePosition.After, null);
            context.InstructionWriter.AttachInstructionSequence(sequence);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, method);
            context.InstructionWriter.DetachInstructionSequence();
        }
    }
}
