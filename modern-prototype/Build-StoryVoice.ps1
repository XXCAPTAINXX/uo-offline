param([string]$PythonPath='python',[string]$Voice='en-GB-SoniaNeural',[string]$Dependencies='')
$ErrorActionPreference='Stop'
$script=Join-Path $PSScriptRoot 'Build-StoryVoiceNeural.py'
if($Dependencies){& $PythonPath $script --voice $Voice --dependencies $Dependencies}else{& $PythonPath $script --voice $Voice}
if($LASTEXITCODE -ne 0){throw 'Neural voice generation failed.'}
