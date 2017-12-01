﻿using Thrifty.Nifty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Thrift;
using Thrift.Protocol;

namespace Thrifty.Nifty.Processors
{
    public static class NiftyProcessorAdapters
    {
        /**
         * Adapt a {@link TProcessor} to a standard Thrift {@link NiftyProcessor}. Nifty uses this
         * internally to adapt the processors generated by the standard Thrift code generator into
         * instances of {@link NiftyProcessor} usable by {@link com.facebook.nifty.core.NiftyDispatcher}
         */
        public static INiftyProcessor ProcessorFromTProcessor(TProcessor standardThriftProcessor)
        {
            CheckProcessMethodSignature();
            return new DelegateNiftyProcessor((pIn, pOut, rContext) => standardThriftProcessor.Process(pIn, pOut));
        }

        /**
         * Create a {@link NiftyProcessorFactory} that always returns the same {@link NiftyProcessor}
         * adapted from the given standard Thrift {@link TProcessor}
         */
        public static INiftyProcessorFactory FactoryFromTProcessor(TProcessor standardThriftProcessor)
        {
            CheckProcessMethodSignature();

            return new DelegateNiftyProcessorFactory(transport => ProcessorFromTProcessor(standardThriftProcessor));
        }

        //**
        // * Create a {@link NiftyProcessorFactory} that delegates to a standard Thrift {@link TProcessorFactory}
        // * to construct an instance, then adapts each instance to a {@link NiftyProcessor}
        // */
        //    public static INiftyProcessorFactory factoryFromTProcessorFactory(TProcessorFactory standardThriftProcessorFactory)
        //{
        //    checkProcessMethodSignature();
        //    return new DelegateNiftyProcessorFactory(t=>processorFromTProcessor(sta)

        //    return new NiftyProcessorFactory()
        //        {
        //            @Override
        //            public NiftyProcessor getProcessor(TTransport transport)
        //{
        //    return processorFromTProcessor(standardThriftProcessorFactory.getProcessor
        //            (transport));
        //}
        //        };
        //    }

        /**
         * Adapt a {@link NiftyProcessor} to a standard Thrift {@link TProcessor}. The {@link
         * com.facebook.nifty.core.NiftyRequestContext} will always be {@code null}
         */
        public static TProcessor ProcessorToTProcessor(INiftyProcessor niftyProcessor)
        {
            return new DelegateTProcessor((inProt, outProt) =>
            {
                try
                {
                    return niftyProcessor.ProcessAsync(inProt, outProt, null).GetAwaiter().GetResult();
                }
                catch (TaskCanceledException cex)
                {
                    throw new NiftyException("nifty processor 线程被取消。", cex);
                }
                catch (Exception ex)
                {
                    throw new NiftyException("niftyProcessor 处理出错。", ex);
                }
            });
        }


        //public static TProcessorFactory processorToTProcessorFactory(INiftyProcessor niftyProcessor)
        //{

        //    return new TProcessorFactory(processorToTProcessor(niftyProcessor));
        //}

        ///// <summary>
        ///// Create a standard thrift {@link TProcessorFactory} that delegates to 
        ///// a {@link NiftyProcessorFactory} to construct an instance, 
        ///// then adapts each instance to a standard Thrift {@link TProcessor}
        ///// </summary>
        ///// <param name="NiftyProcessorFactory"></param>
        ///// <param name=""></param>
        ///// <returns></returns>
        //public static TProcessorFactory processorFactoryToTProcessorFactory(final NiftyProcessorFactory niftyProcessorFactory)
        //{
        //        return new TProcessorFactory(null) {
        //                @Override
        //                public TProcessor getProcessor(TTransport trans)
        //    {
        //        return processorToTProcessor(niftyProcessorFactory.getProcessor(trans));
        //    }
        //};
        //}

        /// <summary>
        /// Catch the mismatch early if someone tries to pass our internal variant of TProcessor with 
        /// a different signature on the process() method into these adapters.
        /// </summary>
        private static void CheckProcessMethodSignature()
        {
            try
            {
                typeof(TProcessor).GetMethod(nameof(TProcessor.Process), new Type[] { typeof(TProtocol), typeof(TProtocol) });
            }
            catch (Exception ex)
            {
                ex.ThrowIfNecessary();
                throw new NiftyException("The loaded TProcessor class is not supported by version of the adapters");
            }
        }
    }
}