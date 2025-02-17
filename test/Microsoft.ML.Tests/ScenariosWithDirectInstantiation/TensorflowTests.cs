﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.Data;
using Microsoft.ML.ImageAnalytics;
using Microsoft.ML.RunTests;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Normalizers;
using Microsoft.ML.Transforms.TensorFlow;
using Xunit;

namespace Microsoft.ML.Scenarios
{
    public partial class ScenariosTests
    {
        private class TestData
        {
            [VectorType(4)]
            public float[] a;
            [VectorType(4)]
            public float[] b;
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformMatrixMultiplicationTest()
        {
            var modelLocation = "model_matmul/frozen_saved_model.pb";
            var mlContext = new MLContext(seed: 1, conc: 1);
            // Pipeline
            var loader = mlContext.Data.ReadFromEnumerable(
                    new List<TestData>(new TestData[] {
                        new TestData() { a = new[] { 1.0f, 2.0f,
                                                     3.0f, 4.0f },
                                         b = new[] { 1.0f, 2.0f,
                                                     3.0f, 4.0f } },
                        new TestData() { a = new[] { 2.0f, 2.0f,
                                                     2.0f, 2.0f },
                                         b = new[] { 3.0f, 3.0f,
                                                     3.0f, 3.0f } } }));
            var trans = new TensorFlowTransformer(mlContext, modelLocation, new[] { "a", "b" }, new[] { "c" }).Transform(loader);

            using (var cursor = trans.GetRowCursorForAllColumns())
            {
                var cgetter = cursor.GetGetter<VBuffer<float>>(2);
                Assert.True(cursor.MoveNext());
                VBuffer<float> c = default;
                cgetter(ref c);

                var cValues = c.GetValues();
                Assert.Equal(1.0 * 1.0 + 2.0 * 3.0, cValues[0]);
                Assert.Equal(1.0 * 2.0 + 2.0 * 4.0, cValues[1]);
                Assert.Equal(3.0 * 1.0 + 4.0 * 3.0, cValues[2]);
                Assert.Equal(3.0 * 2.0 + 4.0 * 4.0, cValues[3]);

                Assert.True(cursor.MoveNext());
                c = default;
                cgetter(ref c);

                cValues = c.GetValues();
                Assert.Equal(2.0 * 3.0 + 2.0 * 3.0, cValues[0]);
                Assert.Equal(2.0 * 3.0 + 2.0 * 3.0, cValues[1]);
                Assert.Equal(2.0 * 3.0 + 2.0 * 3.0, cValues[2]);
                Assert.Equal(2.0 * 3.0 + 2.0 * 3.0, cValues[3]);

                Assert.False(cursor.MoveNext());
            }
        }

        private class TypesData
        {
            [VectorType(2)]
            public double[] f64;
            [VectorType(2)]
            public float[] f32;
            [VectorType(2)]
            public long[] i64;
            [VectorType(2)]
            public int[] i32;
            [VectorType(2)]
            public short[] i16;
            [VectorType(2)]
            public sbyte[] i8;
            [VectorType(2)]
            public ulong[] u64;
            [VectorType(2)]
            public uint[] u32;
            [VectorType(2)]
            public ushort[] u16;
            [VectorType(2)]
            public byte[] u8;
            [VectorType(2)]
            public bool[] b;
        }

        /// <summary>
        /// Test to ensure the supported datatypes can passed to TensorFlow .
        /// </summary>
        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformInputOutputTypesTest()
        {
            // This an identity model which returns the same output as input.
            var model_location = "model_types_test";

            //Data
            var data = new List<TypesData>(
                        new TypesData[] {
                            new TypesData() {   f64 = new[] { -1.0, 2.0 },
                                                f32 = new[] { -1.0f, 2.0f },
                                                i64 = new[] { -1L, 2 },
                                                i32 = new[] { -1, 2 },
                                                i16 = new short[] { -1, 2 },
                                                i8 = new sbyte[] { -1, 2 },
                                                u64 = new ulong[] { 1, 2 },
                                                u32 = new uint[] { 1, 2 },
                                                u16 = new ushort[] { 1, 2 },
                                                u8 = new byte[] { 1, 2 },
                                                b = new bool[] { true, true },
                            },
                           new TypesData() {   f64 = new[] { -3.0, 4.0 },
                                                f32 = new[] { -3.0f, 4.0f },
                                                i64 = new[] { -3L, 4 },
                                                i32 = new[] { -3, 4 },
                                                i16 = new short[] { -3, 4 },
                                                i8 = new sbyte[] { -3, 4 },
                                                u64 = new ulong[] { 3, 4 },
                                                u32 = new uint[] { 3, 4 },
                                                u16 = new ushort[] { 3, 4 },
                                                u8 = new byte[] { 3, 4 },
                                                b = new bool[] { false, false },
                            } });

            var mlContext = new MLContext(seed: 1, conc: 1);
            // Pipeline

            var loader = mlContext.Data.ReadFromEnumerable(data);

            var inputs = new string[]{"f64", "f32", "i64", "i32", "i16", "i8", "u64", "u32", "u16", "u8","b"};
            var outputs = new string[] { "o_f64", "o_f32", "o_i64", "o_i32", "o_i16", "o_i8", "o_u64", "o_u32", "o_u16", "o_u8", "o_b" };
            var trans = new TensorFlowTransformer(mlContext, model_location, inputs, outputs).Transform(loader); ;

            using (var cursor = trans.GetRowCursorForAllColumns())
            {
                var f64getter = cursor.GetGetter<VBuffer<double>>(11);
                var f32getter = cursor.GetGetter<VBuffer<float>>(12);
                var i64getter = cursor.GetGetter<VBuffer<long>>(13);
                var i32getter = cursor.GetGetter<VBuffer<int>>(14);
                var i16getter = cursor.GetGetter<VBuffer<short>>(15);
                var i8getter = cursor.GetGetter<VBuffer<sbyte>>(16);
                var u64getter = cursor.GetGetter<VBuffer<ulong>>(17);
                var u32getter = cursor.GetGetter<VBuffer<uint>>(18);
                var u16getter = cursor.GetGetter<VBuffer<ushort>>(19);
                var u8getter = cursor.GetGetter<VBuffer<byte>>(20);
                var boolgetter = cursor.GetGetter<VBuffer<bool>>(21);

               
                VBuffer<double> f64 = default;
                VBuffer<float> f32 = default;
                VBuffer<long> i64 = default;
                VBuffer<int> i32 = default;
                VBuffer<short> i16 = default;
                VBuffer<sbyte> i8 = default;
                VBuffer<ulong> u64 = default;
                VBuffer<uint> u32 = default;
                VBuffer<ushort> u16 = default;
                VBuffer<byte> u8 = default;
                VBuffer<bool> b = default;
                foreach (var sample in data)
                {
                    Assert.True(cursor.MoveNext());

                    f64getter(ref f64);
                    f32getter(ref f32);
                    i64getter(ref i64);
                    i32getter(ref i32);
                    i16getter(ref i16);
                    i8getter(ref i8);
                    u64getter(ref u64);
                    u32getter(ref u32);
                    u16getter(ref u16);
                    u8getter(ref u8);
                    u8getter(ref u8);
                    boolgetter(ref b);

                    var f64Values = f64.GetValues();
                    Assert.Equal(2, f64Values.Length);
                    Assert.True(f64Values.SequenceEqual(sample.f64));
                    var f32Values = f32.GetValues();
                    Assert.Equal(2, f32Values.Length);
                    Assert.True(f32Values.SequenceEqual(sample.f32));
                    var i64Values = i64.GetValues();
                    Assert.Equal(2, i64Values.Length);
                    Assert.True(i64Values.SequenceEqual(sample.i64));
                    var i32Values = i32.GetValues();
                    Assert.Equal(2, i32Values.Length);
                    Assert.True(i32Values.SequenceEqual(sample.i32));
                    var i16Values = i16.GetValues();
                    Assert.Equal(2, i16Values.Length);
                    Assert.True(i16Values.SequenceEqual(sample.i16));
                    var i8Values = i8.GetValues();
                    Assert.Equal(2, i8Values.Length);
                    Assert.True(i8Values.SequenceEqual(sample.i8));
                    var u64Values = u64.GetValues();
                    Assert.Equal(2, u64Values.Length);
                    Assert.True(u64Values.SequenceEqual(sample.u64));
                    var u32Values = u32.GetValues();
                    Assert.Equal(2, u32Values.Length);
                    Assert.True(u32Values.SequenceEqual(sample.u32));
                    var u16Values = u16.GetValues();
                    Assert.Equal(2, u16Values.Length);
                    Assert.True(u16Values.SequenceEqual(sample.u16));
                    var u8Values = u8.GetValues();
                    Assert.Equal(2, u8Values.Length);
                    Assert.True(u8Values.SequenceEqual(sample.u8));
                    var bValues = b.GetValues();
                    Assert.Equal(2, bValues.Length);
                    Assert.True(bValues.SequenceEqual(sample.b));
                }
                Assert.False(cursor.MoveNext());
            }
        }

        [Fact(Skip = "Model files are not available yet")]
        public void TensorFlowTransformObjectDetectionTest()
        {
            var modelLocation = @"C:\models\TensorFlow\ssd_mobilenet_v1_coco_2018_01_28\frozen_inference_graph.pb";
            var mlContext = new MLContext(seed: 1, conc: 1);
            var dataFile = GetDataPath("images/images.tsv");
            var imageFolder = Path.GetDirectoryName(dataFile);
            var data = mlContext.CreateLoader("Text{col=ImagePath:TX:0 col=Name:TX:1}", new MultiFileSource(dataFile));
            var images = new ImageLoaderTransformer(mlContext, imageFolder, ("ImagePath", "ImageReal")).Transform(data);
            var cropped = new ImageResizerTransformer(mlContext, "ImageReal", "ImageCropped", 32, 32).Transform(images);

            var pixels = new ImagePixelExtractorTransformer(mlContext, "ImageCropped", "image_tensor", asFloat: false).Transform(cropped);
            var tf = new TensorFlowTransformer(mlContext, modelLocation, new[] { "image_tensor" },
                new[] { "detection_boxes", "detection_scores", "num_detections", "detection_classes" }).Transform(pixels);

            tf.Schema.TryGetColumnIndex("image_tensor", out int input);
            tf.Schema.TryGetColumnIndex("detection_boxes", out int boxes);
            tf.Schema.TryGetColumnIndex("detection_scores", out int scores);
            tf.Schema.TryGetColumnIndex("num_detections", out int num);
            tf.Schema.TryGetColumnIndex("detection_classes", out int classes);

            using (var curs = tf.GetRowCursor(tf.Schema["image_tensor"], tf.Schema["detection_boxes"], tf.Schema["detection_scores"], tf.Schema["detection_classes"], tf.Schema["num_detections"]))
            {
                var getInput = curs.GetGetter<VBuffer<byte>>(input);
                var getBoxes = curs.GetGetter<VBuffer<float>>(boxes);
                var getScores = curs.GetGetter<VBuffer<float>>(scores);
                var getNum = curs.GetGetter<VBuffer<float>>(num);
                var getClasses = curs.GetGetter<VBuffer<float>>(classes);
                var buffer = default(VBuffer<float>);
                var inputBuffer = default(VBuffer<byte>);
                while (curs.MoveNext())
                {
                    getInput(ref inputBuffer);
                    getBoxes(ref buffer);
                    getScores(ref buffer);
                    getNum(ref buffer);
                    getClasses(ref buffer);
                }
            }
        }

        [Fact(Skip = "Model files are not available yet")]
        public void TensorFlowTransformInceptionTest()
        {
            var modelLocation = @"C:\models\TensorFlow\tensorflow_inception_graph.pb";
            var mlContext = new MLContext(seed: 1, conc: 1);
            var dataFile = GetDataPath("images/images.tsv");
            var imageFolder = Path.GetDirectoryName(dataFile);
            var data = mlContext.CreateLoader("Text{col=ImagePath:TX:0 col=Name:TX:1}", new MultiFileSource(dataFile));
            var images = new ImageLoaderTransformer(mlContext, imageFolder, ("ImagePath", "ImageReal")).Transform(data);
            var cropped = new ImageResizerTransformer(mlContext, "ImageReal", "ImageCropped", 224, 224).Transform(images);
            var pixels = new ImagePixelExtractorTransformer(mlContext, "ImageCropped", "input").Transform(cropped);
            var tf = new TensorFlowTransformer(mlContext, modelLocation, "input", "softmax2_pre_activation").Transform(pixels);

            tf.Schema.TryGetColumnIndex("input", out int input);
            tf.Schema.TryGetColumnIndex("softmax2_pre_activation", out int b);
            using (var curs = tf.GetRowCursor(tf.Schema["input"], tf.Schema["softmax2_pre_activation"]))
            {
                var get = curs.GetGetter<VBuffer<float>>(b);
                var getInput = curs.GetGetter<VBuffer<float>>(input);
                var buffer = default(VBuffer<float>);
                var inputBuffer = default(VBuffer<float>);
                while (curs.MoveNext())
                {
                    getInput(ref inputBuffer);
                    get(ref buffer);
                }
            }
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowInputsOutputsSchemaTest()
        {
            var mlContext = new MLContext(seed: 1, conc: 1);
            var model_location = "mnist_model/frozen_saved_model.pb";
            var schema = TensorFlowUtils.GetModelSchema(mlContext, model_location);
            Assert.Equal(86, schema.Count);
            Assert.True(schema.TryGetColumnIndex("Placeholder", out int col));
            var type = (VectorType)schema[col].Type;
            Assert.Equal(2, type.Dimensions.Length);
            Assert.Equal(28, type.Dimensions[0]);
            Assert.Equal(28, type.Dimensions[1]);
            var metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowOperatorTypeKind].Type;
            Assert.NotNull(metadataType);
            Assert.True(metadataType is TextType);
            ReadOnlyMemory<char> opType = default;
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowOperatorTypeKind, ref opType);
            Assert.Equal("Placeholder", opType.ToString());
            metadataType = schema[col].Metadata.Schema.GetColumnOrNull(TensorFlowUtils.TensorflowUpstreamOperatorsKind)?.Type;
            Assert.Null(metadataType);

            Assert.True(schema.TryGetColumnIndex("conv2d/Conv2D/ReadVariableOp", out col));
            type = (VectorType)schema[col].Type;
            Assert.Equal(new[] { 5, 5, 1, 32 }, type.Dimensions);
            metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowOperatorTypeKind].Type;
            Assert.NotNull(metadataType);
            Assert.True(metadataType is TextType);
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowOperatorTypeKind, ref opType);
            Assert.Equal("Identity", opType.ToString());
            metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowUpstreamOperatorsKind].Type;
            Assert.NotNull(metadataType);
            VBuffer<ReadOnlyMemory<char>> inputOps = default;
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowUpstreamOperatorsKind, ref inputOps);
            Assert.Equal(1, inputOps.Length);
            Assert.Equal("conv2d/kernel", inputOps.GetValues()[0].ToString());

            Assert.True(schema.TryGetColumnIndex("conv2d/Conv2D", out col));
            type = (VectorType)schema[col].Type;
            Assert.Equal(new[] { 28, 28, 32 }, type.Dimensions);
            metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowOperatorTypeKind].Type;
            Assert.NotNull(metadataType);
            Assert.True(metadataType is TextType);
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowOperatorTypeKind, ref opType);
            Assert.Equal("Conv2D", opType.ToString());
            metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowUpstreamOperatorsKind].Type;
            Assert.NotNull(metadataType);
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowUpstreamOperatorsKind, ref inputOps);
            Assert.Equal(2, inputOps.Length);
            Assert.Equal("reshape/Reshape", inputOps.GetValues()[0].ToString());
            Assert.Equal("conv2d/Conv2D/ReadVariableOp", inputOps.GetValues()[1].ToString());

            Assert.True(schema.TryGetColumnIndex("Softmax", out col));
            type = (VectorType)schema[col].Type;
            Assert.Equal(new[] { 10 }, type.Dimensions);
            metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowOperatorTypeKind].Type;
            Assert.NotNull(metadataType);
            Assert.True(metadataType is TextType);
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowOperatorTypeKind, ref opType);
            Assert.Equal("Softmax", opType.ToString());
            metadataType = schema[col].Metadata.Schema[TensorFlowUtils.TensorflowUpstreamOperatorsKind].Type;
            Assert.NotNull(metadataType);
            schema[col].Metadata.GetValue(TensorFlowUtils.TensorflowUpstreamOperatorsKind, ref inputOps);
            Assert.Equal(1, inputOps.Length);
            Assert.Equal("sequential/dense_1/BiasAdd", inputOps.GetValues()[0].ToString());

            model_location = "model_matmul/frozen_saved_model.pb";
            schema = TensorFlowUtils.GetModelSchema(mlContext, model_location);
            char name = 'a';
            for (int i = 0; i < schema.Count; i++)
            {
                Assert.Equal(name.ToString(), schema[i].Name);
                type = (VectorType)schema[i].Type;
                Assert.Equal(new[] { 2, 2 }, type.Dimensions);
                name++;
            }
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformMNISTConvTest()
        {
            var mlContext = new MLContext(seed: 1, conc: 1);
            var reader = mlContext.Data.CreateTextLoader(
                    columns: new[]
                    {
                        new TextLoader.Column("Label", DataKind.U4 , new [] { new TextLoader.Range(0) }, new KeyRange(0, 9)),
                        new TextLoader.Column("Placeholder", DataKind.R4, new []{ new TextLoader.Range(1, 784) })

                    },
                    hasHeader: true
                );

            var trainData = reader.Read(GetDataPath(TestDatasets.mnistTiny28.trainFilename));
            var testData = reader.Read(GetDataPath(TestDatasets.mnistOneClass.testFilename));

            var pipe = mlContext.Transforms.CopyColumns(("Placeholder", "reshape_input"))
                .Append(new TensorFlowEstimator(mlContext, "mnist_model/frozen_saved_model.pb", new[] { "Placeholder", "reshape_input" }, new[] { "Softmax", "dense/Relu" }))
                .Append(mlContext.Transforms.Concatenate("Features", "Softmax", "dense/Relu"))
                .Append(mlContext.MulticlassClassification.Trainers.LightGbm("Label", "Features"));

            var trainedModel = pipe.Fit(trainData);
            var predicted = trainedModel.Transform(testData);
            var metrics = mlContext.MulticlassClassification.Evaluate(predicted);

            Assert.Equal(0.99, metrics.AccuracyMicro, 2);
            Assert.Equal(1.0, metrics.AccuracyMacro, 2);

            var oneSample = GetOneMNISTExample();

            var predictFunction = trainedModel.CreatePredictionEngine<MNISTData, MNISTPrediction>(mlContext);

            var onePrediction = predictFunction.Predict(oneSample);

            Assert.Equal(5, GetMaxIndexForOnePrediction(onePrediction));
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformMNISTLRTrainingTest()
        {
            const double expectedMicroAccuracy = 0.72173913043478266;
            const double expectedMacroAccruacy = 0.67482993197278918;
            var model_location = "mnist_lr_model";
            try
            {
                var mlContext = new MLContext(seed: 1, conc: 1);
                var reader = mlContext.Data.CreateTextLoader(columns: new[]
                        {
                            new TextLoader.Column("Label", DataKind.I8, 0),
                            new TextLoader.Column("Placeholder", DataKind.R4, new []{ new TextLoader.Range(1, 784) })
                        }
                    );

                var trainData = reader.Read(GetDataPath(TestDatasets.mnistTiny28.trainFilename));
                var testData = reader.Read(GetDataPath(TestDatasets.mnistOneClass.testFilename));

                var pipe = mlContext.Transforms.Categorical.OneHotEncoding("Label", "OneHotLabel")
                    .Append(mlContext.Transforms.Normalize(new NormalizingEstimator.MinMaxColumn("Placeholder", "Features")))
                    .Append(new TensorFlowEstimator(mlContext, new TensorFlowTransformer.Arguments()
                    {
                        ModelLocation = model_location,
                        InputColumns = new[] { "Features" },
                        OutputColumns = new[] { "Prediction", "b" },
                        LabelColumn = "OneHotLabel",
                        TensorFlowLabel = "Label",
                        OptimizationOperation = "SGDOptimizer",
                        LossOperation = "Loss",
                        Epoch = 10,
                        LearningRateOperation = "SGDOptimizer/learning_rate",
                        LearningRate = 0.001f,
                        BatchSize = 20,
                        ReTrain = true
                    }))
                    .Append(mlContext.Transforms.Concatenate("Features", "Prediction"))
                    .Append(mlContext.Transforms.Conversion.MapValueToKey("Label", "KeyLabel", maxNumTerms: 10))
                    .Append(mlContext.MulticlassClassification.Trainers.LightGbm("KeyLabel", "Features"));

                var trainedModel = pipe.Fit(trainData);
                var predicted = trainedModel.Transform(testData);
                var metrics = mlContext.MulticlassClassification.Evaluate(predicted, label: "KeyLabel");
                Assert.InRange(metrics.AccuracyMicro, expectedMicroAccuracy, 1);
                Assert.InRange(metrics.AccuracyMacro, expectedMacroAccruacy, 1);
                var predictionFunction = trainedModel.CreatePredictionEngine<MNISTData, MNISTPrediction>(mlContext);

                var oneSample = GetOneMNISTExample();
                var onePrediction = predictionFunction.Predict(oneSample);
                Assert.Equal(0, GetMaxIndexForOnePrediction(onePrediction));


                var trainDataTransformed = trainedModel.Transform(trainData);
                using (var cursor = trainDataTransformed.GetRowCursorForAllColumns())
                {
                    trainDataTransformed.Schema.TryGetColumnIndex("b", out int bias);
                    var getter = cursor.GetGetter<VBuffer<float>>(bias);
                    if (cursor.MoveNext())
                    {
                        var trainedBias = default(VBuffer<float>);
                        getter(ref trainedBias);
                        Assert.NotEqual(trainedBias.GetValues().ToArray(), new float[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f });
                    }
                }
            }
            finally
            {
                // This test changes the state of the model.
                // Cleanup folder so that other test can also use the same model.
                CleanUp(model_location);
            }
        }

        private void CleanUp(string model_location)
        {
            var directories = Directory.GetDirectories(model_location, "variables-*");
            if (directories != null && directories.Length > 0)
            {
                var varDir = Path.Combine(model_location, "variables");
                if (Directory.Exists(varDir))
                    Directory.Delete(varDir, true);
                Directory.Move(directories[0], varDir);
            }
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformMNISTConvTrainingTest()
        {
            ExecuteTFTransformMNISTConvTrainingTest(false, null, 0.74782608695652175, 0.608843537414966);
            ExecuteTFTransformMNISTConvTrainingTest(true, 5, 0.75652173913043474, 0.610204081632653);
        }

        private void ExecuteTFTransformMNISTConvTrainingTest(bool shuffle, int? shuffleSeed, double expectedMicroAccuracy, double expectedMacroAccruacy)
        {
            const string modelLocation = "mnist_conv_model";
            try
            {
                var mlContext = new MLContext(seed: 1, conc: 1);

                var reader = mlContext.Data.CreateTextLoader(new[]
                    {
                        new TextLoader.Column("Label", DataKind.U4, new []{ new TextLoader.Range(0) }, new KeyRange(0, 9)),
                        new TextLoader.Column("TfLabel", DataKind.I8, 0),
                        new TextLoader.Column("Placeholder", DataKind.R4, new []{ new TextLoader.Range(1, 784) })
                    }
                );

                var trainData = reader.Read(GetDataPath(TestDatasets.mnistTiny28.trainFilename));
                var testData = reader.Read(GetDataPath(TestDatasets.mnistOneClass.testFilename));

                IDataView preprocessedTrainData = null;
                IDataView preprocessedTestData = null;
                if (shuffle)
                {
                    // Shuffle training data set
                    preprocessedTrainData = new RowShufflingTransformer(mlContext, new RowShufflingTransformer.Arguments()
                    {
                        ForceShuffle = shuffle,
                        ForceShuffleSeed = shuffleSeed
                    }, trainData);

                    // Shuffle test data set
                    preprocessedTestData = new RowShufflingTransformer(mlContext, new RowShufflingTransformer.Arguments()
                    {
                        ForceShuffle = shuffle,
                        ForceShuffleSeed = shuffleSeed
                    }, testData);
                }
                else
                {
                    preprocessedTrainData = trainData;
                    preprocessedTestData = testData;
                }

                var pipe = mlContext.Transforms.CopyColumns(("Placeholder", "Features"))
                    .Append(new TensorFlowEstimator(mlContext, new TensorFlowTransformer.Arguments()
                    {
                        ModelLocation = modelLocation,
                        InputColumns = new[] { "Features" },
                        OutputColumns = new[] { "Prediction" },
                        LabelColumn = "TfLabel",
                        TensorFlowLabel = "Label",
                        OptimizationOperation = "MomentumOp",
                        LossOperation = "Loss",
                        MetricOperation = "Accuracy",
                        Epoch = 10,
                        LearningRateOperation = "learning_rate",
                        LearningRate = 0.01f,
                        BatchSize = 20,
                        ReTrain = true
                    }))
                    .Append(mlContext.Transforms.Concatenate("Features", "Prediction"))
                    .AppendCacheCheckpoint(mlContext)
                    .Append(mlContext.MulticlassClassification.Trainers.LightGbm("Label", "Features"));

                var trainedModel = pipe.Fit(preprocessedTrainData);
                var predicted = trainedModel.Transform(preprocessedTestData);
                var metrics = mlContext.MulticlassClassification.Evaluate(predicted);

                // First group of checks. They check if the overall prediction quality is ok using a test set.
                Assert.InRange(metrics.AccuracyMicro, expectedMicroAccuracy - .01, expectedMicroAccuracy + .01);
                Assert.InRange(metrics.AccuracyMacro, expectedMacroAccruacy - .01, expectedMicroAccuracy + .01);

                // Create prediction function and test prediction
                var predictFunction = trainedModel.CreatePredictionEngine<MNISTData, MNISTPrediction>(mlContext);

                var oneSample = GetOneMNISTExample();

                var prediction = predictFunction.Predict(oneSample);

                Assert.Equal(5, GetMaxIndexForOnePrediction(prediction));
            }
            finally
            {
                // This test changes the state of the model.
                // Cleanup folder so that other test can also use the same model.
                CleanUp(modelLocation);
            }
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformMNISTConvSavedModelTest()
        {
            // This test trains a multi-class classifier pipeline where a pre-trained Tenroflow model is used for featurization.
            // Two group of test criteria are checked. One group contains micro and macro accuracies. The other group is the range
            // of predicted label of a single in-memory example.

            var mlContext = new MLContext(seed: 1, conc: 1);
            var reader = mlContext.Data.CreateTextLoader(columns: new[]
                {
                    new TextLoader.Column("Label", DataKind.U4 , new [] { new TextLoader.Range(0) }, new KeyRange(0, 9)),
                    new TextLoader.Column("Placeholder", DataKind.R4, new []{ new TextLoader.Range(1, 784) })
                },
                hasHeader: true
            );

            var trainData = reader.Read(GetDataPath(TestDatasets.mnistTiny28.trainFilename));
            var testData = reader.Read(GetDataPath(TestDatasets.mnistOneClass.testFilename));

            var pipe = mlContext.Transforms.CopyColumns(("Placeholder", "reshape_input"))
                .Append(new TensorFlowEstimator(mlContext, "mnist_model", new[] { "Placeholder", "reshape_input" }, new[] { "Softmax", "dense/Relu" }))
                .Append(mlContext.Transforms.Concatenate("Features", new[] { "Softmax", "dense/Relu" }))
                .Append(mlContext.MulticlassClassification.Trainers.LightGbm("Label", "Features"));

            var trainedModel = pipe.Fit(trainData);
            var predicted = trainedModel.Transform(testData);
            var metrics = mlContext.MulticlassClassification.Evaluate(predicted);

            // First group of checks
            Assert.Equal(0.99, metrics.AccuracyMicro, 2);
            Assert.Equal(1.0, metrics.AccuracyMacro, 2);

            // An in-memory example. Its label is predicted below.
            var oneSample = GetOneMNISTExample();

            var predictFunction = trainedModel.CreatePredictionEngine<MNISTData, MNISTPrediction>(mlContext);

            var onePrediction = predictFunction.Predict(oneSample);

            // Second group of checks
            Assert.Equal(5, GetMaxIndexForOnePrediction(onePrediction));
        }

        private MNISTData GetOneMNISTExample()
        {
            return new MNISTData()
            {
                Placeholder = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 18, 18, 18, 126,
                136, 175, 26, 166, 255, 247, 127, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 30, 36, 94, 154, 170, 253, 253, 253, 253, 253, 225, 172,
                253, 242, 195, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 49, 238,
                253, 253, 253, 253, 253, 253, 253, 253, 251, 93, 82, 82, 56,
                39, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 18, 219, 253, 253, 253,
                253, 253, 198, 182, 247, 241, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 80, 156, 107, 253, 253, 205, 11, 0, 43,
                154, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                14, 1, 154, 253, 90, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 139, 253, 190, 2, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 11,
                190, 253, 70, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 35, 241, 225, 160, 108, 1, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 81,
                240, 253, 253, 119, 25, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 45, 186, 253, 253, 150, 27, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                16, 93, 252, 253, 187, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 249, 253, 249, 64, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 46, 130,
                183, 253, 253, 207, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 39, 148, 229, 253, 253, 253, 250, 182, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24, 114, 221,
                253, 253, 253, 253, 201, 78, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 23, 66, 213, 253, 253, 253, 253, 198, 81, 2,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 18, 171, 219,
                253, 253, 253, 253, 195, 80, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 55, 172, 226, 253, 253, 253, 253, 244, 133,
                11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 136,
                253, 253, 253, 212, 135, 132, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0 }
            };
        }

        private int GetMaxIndexForOnePrediction(MNISTPrediction onePrediction)
        {
            float maxLabel = -1;
            int maxIndex = -1;
            for (int i = 0; i < onePrediction.PredictedLabels.Length; i++)
            {
                if (onePrediction.PredictedLabels[i] > maxLabel)
                {
                    maxLabel = onePrediction.PredictedLabels[i];
                    maxIndex = i;
                }
            }
            return maxIndex;
        }

        public class MNISTData
        {
            [Column("0")]
            public long Label;

            [VectorType(784)]
            public float[] Placeholder;
        }

        public class MNISTPrediction
        {
            [ColumnName("Score")]
            public float[] PredictedLabels;
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformCifar()
        {
            var modelLocation = "cifar_model/frozen_model.pb";

            var mlContext = new MLContext(seed: 1, conc: 1);
            var tensorFlowModel = TensorFlowUtils.LoadTensorFlowModel(mlContext, modelLocation);
            var schema = tensorFlowModel.GetInputSchema();
            Assert.True(schema.TryGetColumnIndex("Input", out int column));
            var type = (VectorType)schema[column].Type;
            var imageHeight = type.Dimensions[0];
            var imageWidth = type.Dimensions[1];

            var dataFile = GetDataPath("images/images.tsv");
            var imageFolder = Path.GetDirectoryName(dataFile);
            var data = mlContext.Data.ReadFromTextFile(dataFile,
                    columns: new[]
                    {
                        new TextLoader.Column("ImagePath", DataKind.TX, 0),
                        new TextLoader.Column("Name", DataKind.TX, 1),
                    }
                );

            var pipeEstimator = new ImageLoadingEstimator(mlContext, imageFolder, ("ImagePath", "ImageReal"))
                .Append(new ImageResizingEstimator(mlContext, "ImageReal", "ImageCropped", imageWidth, imageHeight))
                .Append(new ImagePixelExtractingEstimator(mlContext, "ImageCropped", "Input", interleave: true));

            var pixels = pipeEstimator.Fit(data).Transform(data);
            IDataView trans = new TensorFlowTransformer(mlContext, tensorFlowModel, "Input", "Output").Transform(pixels);

            trans.Schema.TryGetColumnIndex("Output", out int output);
            using (var cursor = trans.GetRowCursor(trans.Schema["Output"]))
            {
                var buffer = default(VBuffer<float>);
                var getter = cursor.GetGetter<VBuffer<float>>(output);
                var numRows = 0;
                while (cursor.MoveNext())
                {
                    getter(ref buffer);
                    Assert.Equal(10, buffer.Length);
                    numRows += 1;
                }
                Assert.Equal(4, numRows);
            }
        }

        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowTransformCifarSavedModel()
        {
            var modelLocation = "cifar_saved_model";
            var mlContext = new MLContext(seed: 1, conc: 1);
            var tensorFlowModel = TensorFlowUtils.LoadTensorFlowModel(mlContext, modelLocation);
            var schema = tensorFlowModel.GetInputSchema();
            Assert.True(schema.TryGetColumnIndex("Input", out int column));
            var type = (VectorType)schema[column].Type;
            var imageHeight = type.Dimensions[0];
            var imageWidth = type.Dimensions[1];

            var dataFile = GetDataPath("images/images.tsv");
            var imageFolder = Path.GetDirectoryName(dataFile);
            var data = mlContext.Data.ReadFromTextFile(dataFile, columns: new[]
                {
                        new TextLoader.Column("ImagePath", DataKind.TX, 0),
                        new TextLoader.Column("Name", DataKind.TX, 1),
                }
            );
            var images = new ImageLoaderTransformer(mlContext, imageFolder, ("ImagePath", "ImageReal")).Transform(data);
            var cropped = new ImageResizerTransformer(mlContext, "ImageReal", "ImageCropped", imageWidth, imageHeight).Transform(images);
            var pixels = new ImagePixelExtractorTransformer(mlContext, "ImageCropped", "Input", interleave: true).Transform(cropped);
            IDataView trans = new TensorFlowTransformer(mlContext, tensorFlowModel, "Input", "Output").Transform(pixels);

            trans.Schema.TryGetColumnIndex("Output", out int output);
            using (var cursor = trans.GetRowCursorForAllColumns())
            {
                var buffer = default(VBuffer<float>);
                var getter = cursor.GetGetter<VBuffer<float>>(output);
                var numRows = 0;
                while (cursor.MoveNext())
                {
                    getter(ref buffer);
                    Assert.Equal(10, buffer.Length);
                    numRows += 1;
                }
                Assert.Equal(4, numRows);
            }
        }

        // This test has been created as result of https://github.com/dotnet/machinelearning/issues/2156.
        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))] // TensorFlow is 64-bit only
        public void TensorFlowGettingSchemaMultipleTimes()
        {
            var modelLocation = "cifar_saved_model";
            var mlContext = new MLContext(seed: 1, conc: 1);
            for (int i = 0; i < 10; i++)
            {
                var schema = TensorFlowUtils.GetModelSchema(mlContext, modelLocation);
                Assert.NotNull(schema);
            }
        }


        [ConditionalFact(typeof(Environment), nameof(Environment.Is64BitProcess))]
        public void TensorFlowTransformCifarInvalidShape()
        {
            var modelLocation = "cifar_model/frozen_model.pb";

            var mlContext = new MLContext(seed: 1, conc: 1);
            var imageHeight = 28;
            var imageWidth = 28;
            var dataFile = GetDataPath("images/images.tsv");
            var imageFolder = Path.GetDirectoryName(dataFile);
            var data = mlContext.Data.ReadFromTextFile(dataFile,
                columns: new[]
                {
                        new TextLoader.Column("ImagePath", DataKind.TX, 0),
                        new TextLoader.Column("Name", DataKind.TX, 1),
                }
            );
            var images = new ImageLoaderTransformer(mlContext, imageFolder, ("ImagePath", "ImageReal")).Transform(data);
            var cropped = new ImageResizerTransformer(mlContext, "ImageReal", "ImageCropped", imageWidth, imageHeight).Transform(images);
            var pixels = new ImagePixelExtractorTransformer(mlContext, "ImageCropped", "Input").Transform(cropped);

            var thrown = false;
            try
            {
                IDataView trans = new TensorFlowTransformer(mlContext, modelLocation, "Input", "Output").Transform(pixels);
            }
            catch
            {
                thrown = true;
            }
            Assert.True(thrown);
        }
    }
}
