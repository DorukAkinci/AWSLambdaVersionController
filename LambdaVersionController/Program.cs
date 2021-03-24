using System;
using CommandLine;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq;

namespace LambdaVersionController
{
    class Program
    {
        static SharedCredentialsFile _sharedCredentialsFile;
        static AmazonLambdaClient _lambdaClient;
        static bool _debugMode = false;
        public class Options
        {
            [Option(Required = true, HelpText = "How many versions do you want to backup?")]
            public int Count { get; set; }

            [Option(Required = false, HelpText = "AWS Profile Name")]
            public string? Profile { get; set; }

            [Option(Required = true, HelpText = "AWS Region Code")]
            public string Region { get; set; }

            [Option(Default = false, HelpText = "Debug Mode ( No deletion )")]
            public bool Debug { get; set; }
        }

        static async Task Main(string[] args)
        {
            try
            {
                _sharedCredentialsFile = new SharedCredentialsFile();

                await Parser.Default.ParseArguments<Options>(args).MapResult(async
                    x =>
                {
                    await RunOptions(x);
                },
                    errors => Task.FromResult(0)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERR: " + ex.Message);
            }
        }

        static async Task RunOptions(Options opts)
        {
            //handle options
            Console.WriteLine("Profile: " + opts.Profile);
            Console.WriteLine("Region: " + opts.Region);
            Console.WriteLine("Count: " + opts.Count.ToString());
            Console.WriteLine("DebugMode: " + opts.Debug.ToString());

            _debugMode = opts.Debug;

            InitializeLambdaClient(opts.Profile, opts.Region);

            await ExecuteVersionDeletion(opts.Count);
        }

        private static async Task ExecuteVersionDeletion(int count)
        {
            var _functionList = await GetAllFunctions();

            foreach (var _function in _functionList)
            {
                Console.WriteLine("-------------------");
                Console.WriteLine("Function Name: " + _function.FunctionName);

                await DeleteOldVersionsForTheFunction(_function.FunctionName, count);
            }
        }

        private static async Task DeleteOldVersionsForTheFunction(string functionName, int count)
        {
            var _versionList = (await _lambdaClient.ListVersionsByFunctionAsync(new ListVersionsByFunctionRequest { FunctionName = functionName }))
                                .Versions
                                .Where(q => !(q.FunctionArn.EndsWith("LATEST")))
                                .OrderByDescending(q => q.LastModified);
            Console.WriteLine("The Function has " + _versionList.Count() + " versions");
            if (_versionList.Count() > count)
            {
                foreach (var _version in _versionList.Skip(count))
                {
                    Console.WriteLine("Deleting " + _version.FunctionArn);
                    if (!_debugMode)
                        await _lambdaClient.DeleteFunctionAsync(new DeleteFunctionRequest { FunctionName = _version.FunctionName, Qualifier = _version.Version });
                }
            }
            else
                Console.WriteLine("The Version Count is lower than " + count + ". The function is skipped.");
        }

        private static void InitializeLambdaClient(string? Profile, string Region)
        {
            if (Profile != null)
                _lambdaClient = new AmazonLambdaClient(GetAWSCredentialProfile(Profile).Options.AccessKey, GetAWSCredentialProfile(Profile).Options.SecretKey, RegionEndpoint.GetBySystemName(Region));
            else
                _lambdaClient = new AmazonLambdaClient(RegionEndpoint.GetBySystemName(Region));
        }

        private static CredentialProfile GetAWSCredentialProfile(string ProfileName)
        {
            CredentialProfile _credentialProfile;
            if (_sharedCredentialsFile.TryGetProfile(ProfileName, out _credentialProfile))
            {
                return _credentialProfile;
            }
            else
                throw new Exception("There is no profile name as " + ProfileName);
        }

        public static async Task<List<FunctionConfiguration>> GetAllFunctions()
        {
            List<FunctionConfiguration> _functionList = new List<FunctionConfiguration>();
            ListFunctionsResponse _response = new ListFunctionsResponse();
            do
            {
                _response = (await _lambdaClient.ListFunctionsAsync(new ListFunctionsRequest { Marker = _response.NextMarker ?? null }));
                _functionList.AddRange(_response.Functions);
            }
            while (_response.NextMarker != null);

            return _functionList;
        }


    }
}
