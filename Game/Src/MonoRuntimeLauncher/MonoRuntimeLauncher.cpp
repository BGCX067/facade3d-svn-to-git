// Copyright (C) 2006-2010 NeoAxis Group Ltd.
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_NO_DEPRECATE
#include <windows.h>
#include <psapi.h>
#include <stdlib.h>
#include <malloc.h>
#include <memory.h>
#include <tchar.h>
#include <string>

#pragma comment (lib, "psapi.lib")

typedef int mono_main(int argc, char* argv[]);
typedef void mono_set_dirs(const char* assembly_dir, const char* config_dir);

std::wstring GetModuleFullDirectory()
{
	TCHAR moduleFileNameTemp[4096];
	GetModuleFileName(NULL, moduleFileNameTemp, sizeof(moduleFileNameTemp) / sizeof(TCHAR));

	std::wstring moduleFileName = moduleFileNameTemp;

	int index1 = (int)moduleFileName.find_last_of(_T("\\"));
	int index2 = (int)moduleFileName.find_last_of(_T("/"));
	int index = index1 > index2 ? index1 : index2;
	if(index == -1)
		return _T("");

	return moduleFileName.substr(0, index);
}

std::wstring GetModuleBaseName()
{
	HANDLE hProcess = GetCurrentProcess();
	HMODULE hModule = GetModuleHandle( NULL );

	TCHAR moduleBaseName[4096];
	memset(moduleBaseName, 0, sizeof(moduleBaseName));
	GetModuleBaseName( hProcess, hModule, moduleBaseName, sizeof(moduleBaseName) / sizeof(TCHAR) );
	return std::wstring(moduleBaseName);
}

std::string ToUTF8(const std::wstring& str)
{
	std::string result;
	if(!str.empty())
	{
		int size = WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, NULL, 0, NULL, NULL);
		if(size != 0)
		{
			char* aString = (char*)_alloca(size * sizeof(char));
			//char* aString = new char[size];
			if(WideCharToMultiByte(CP_UTF8, 0, str.c_str(), -1, aString, size, NULL, NULL) != 0)
				result = aString;
			//delete[] aString;
		}
	}
	return result;
}

std::wstring ToUTFWide(const std::string& str)
{
	std::wstring result;
	if(!str.empty())
	{
		int size = MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, NULL, 0);
		if(size)
		{
			wchar_t* wString = (wchar_t*)_alloca(size * sizeof(wchar_t));
			//wchar_t* wString = new wchar_t[size];
			if(MultiByteToWideChar(CP_UTF8, 0, str.c_str(), -1, wString, size) != 0)
				result = wString;
			//delete[] wString;
		}
	}
	return result;
}

std::wstring GetDestinationFileName()
{
	std::wstring baseName;

	//get from file name of this executable
	{
		std::wstring moduleBaseName = GetModuleBaseName();

		std::wstring moduleBaseNameLower = moduleBaseName;
		for(int n = 0; n < (int)moduleBaseNameLower.length(); n++)
			moduleBaseNameLower[n] = tolower(moduleBaseNameLower[n]);

		int index = (int)moduleBaseNameLower.find(_T("_mono"));
		if(index == -1)
		{
			MessageBox(0, _T("Invalid executable file name.\n\nDemands file name in format \"{destination base file name}_mono[any characters].exe\"."), 
				_T("Mono launcher error"), 
				MB_OK | MB_ICONEXCLAMATION);
			return _T("");
		}

		baseName = moduleBaseName.substr(0, index);
	}

	return baseName + _T(".exe");
	//return GetModuleFullDirectory() + _T("\\") + baseName + _T(".exe");
}

int APIENTRY _tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
{
	//correct current directory
	SetCurrentDirectory(GetModuleFullDirectory().c_str());

	std::wstring monoRuntimeLocalPath = _T("NativeDlls\\Windows_x86\\MonoRuntime");
	std::wstring monoRuntimeFullPath = GetModuleFullDirectory() + _T("\\") + monoRuntimeLocalPath;

	HMODULE hMonoDllModule = NULL;

	//load mono dll
	{
		std::wstring dllPath = monoRuntimeFullPath + std::wstring(_T("\\bin\\mono.dll"));

		hMonoDllModule = LoadLibrary( dllPath.c_str() );
		if(!hMonoDllModule)
		{
			TCHAR error[4096];
			wsprintf(error, _T("Loading \"%s\" failed."), dllPath.c_str());
			MessageBox(0, error,  _T("Mono launcher error"), MB_OK | MB_ICONEXCLAMATION);
			return -1;
		}
	}

	mono_main* monoMainFunction = (mono_main*)GetProcAddress( hMonoDllModule, "mono_main" );
	if(!monoMainFunction)
	{
		MessageBox(0, _T("No \"mono_main\" procedure."), _T("Mono launcher error"), 
			MB_OK | MB_ICONEXCLAMATION);
		return -1;
	}

	mono_set_dirs* monoSetDirsFunction = (mono_set_dirs*)GetProcAddress( 
		hMonoDllModule, "mono_set_dirs" );
	if(!monoSetDirsFunction)
	{
		MessageBox(0, _T("No \"mono_set_dirs\" procedure."), _T("Mono launcher error"), 
			MB_OK | MB_ICONEXCLAMATION);
		return -1;
	}

	std::wstring destinationFileName = GetDestinationFileName();
	if(destinationFileName.empty())
		return -1;
	std::string destinationFileNameUtf8 = ToUTF8(destinationFileName);

	//it's will be modified
	char* lpCmdLineTemp = new char[wcslen(lpCmdLine) + 1];
	memset(lpCmdLineTemp, 0, wcslen(lpCmdLine) + 1);
	wcstombs(lpCmdLineTemp, lpCmdLine, wcslen(lpCmdLine));

	int argc = 0;
	char* argv[256];
	{
		argv[argc] = "none";
		argc++;

		argv[argc] = "--runtime=v4.0";
		argc++;

		argv[argc] = (char*)destinationFileNameUtf8.c_str();
		argc++;

		//parse windows command line
		char* cmdPointer = lpCmdLineTemp;
		while(*cmdPointer && argc < 256)
		{
			while(*cmdPointer && *cmdPointer <= ' ')
				cmdPointer++;
			if(*cmdPointer)
			{
				argv[argc++] = cmdPointer;
				while(*cmdPointer && *cmdPointer > ' ')
					cmdPointer++;
				if(*cmdPointer) 
					*(cmdPointer++) = 0;
			}
		}
	}

	//run mono

	std::string monoLibPathUtf8 = ToUTF8(monoRuntimeFullPath + _T("\\lib"));
	std::string monoEtcPathUtf8 = ToUTF8(monoRuntimeFullPath + _T("\\etc"));
	//1 - path to "lib" directory
	//2 - config file path (by default in the mono path to "etc" directory)
	monoSetDirsFunction(monoLibPathUtf8.c_str(), monoEtcPathUtf8.c_str());

	int result = monoMainFunction(argc, argv);

	delete[] lpCmdLineTemp;

	return result;
}
