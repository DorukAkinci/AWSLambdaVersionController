pipeline {
    agent {
        docker { 
            image 'mcr.microsoft.com/dotnet/core/sdk:3.1' 
        }
    }
    
    tools {
        git 'git'
    }
    
    parameters { 
          string(name: 'COUNT', defaultValue: '2', description: 'How many versions do you want to backup?')
          string(name: 'REGION', defaultValue: 'eu-west-1', description: 'AWS Region')
    }
    
    environment{
        HOME = '/tmp' 
        DOTNET_CLI_HOME = "/tmp/DOTNET_CLI_HOME"
    }
    
    stages {
        stage('Info') {
           steps {
               sh 'env'
            }
        }
        
        stage('Clone Repo') {
            steps {
                echo 'Cloning Repo'
                sh 'rm -rf repo; mkdir repo'
                dir ('repo') {
                    git branch: "master",
                    //credentialsId: 'XXXXX',
                    url: 'https://github.com/DorukAkinci/AWSLambdaVersionController'
                }
            }
        }

        stage('Build Repo') {
            steps {
                echo 'Building Repo'
                dir ('repo') {
                    sh "ls -la"
                    sh "dotnet restore"
                    sh "dotnet publish -c Release -o Release"
                }
            }
        }
        
        stage('Execute the Application') {
            steps {
                echo 'Executing the Application'
                dir ('repo') {
                    dir ('Release') {
                        sh "ls -la"
                        sh "./LambdaVersionController --region ${REGION} --count ${COUNT}"
                    }
                }
            }
        }
    }
}
