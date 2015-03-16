//
// ScenePanel.xaml.cpp
// Implementation of the ScenePanel class
//

#include "pch.h"
#include "ScenePanel.xaml.h"

using namespace DirectXSceneStore;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Graphics::Display;
using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;
using namespace Concurrency;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

ScenePanel::ScenePanel()
{
	InitializeComponent();

	DXSwapChainPanel->CompositionScaleChanged += 
		ref new TypedEventHandler<SwapChainPanel ^, 
		Object ^>(this, &ScenePanel::OnCompositionScaleChanged);

	DXSwapChainPanel->SizeChanged += ref new Windows::UI::Xaml::SizeChangedEventHandler(this, &ScenePanel::OnSwapChainPanelSizeChanged);

	// At this point we have access to the device.
	// We can create the device-dependent resources.
	m_deviceResources = std::make_shared<DX::DeviceResources>();
	m_deviceResources->SetSwapChainPanel(DXSwapChainPanel);

	m_main = std::unique_ptr<GameMain>(new GameMain(m_deviceResources, this));

	m_main->StartRenderLoop();
}

void ScenePanel::OnSuspending()
{

}

void ScenePanel::OnResuming()
{

}

void ScenePanel::OnGameInfoOverlayTapped(Object^ /* sender */, TappedRoutedEventArgs^ /* args */)
{
	m_main->PressComplete();
}

//---------------- IGameUIControl methods--------------------

void ScenePanel::SetGameLoading()
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this]()
	{
		GameInfoOverlayTitle->Text = "Loading Resources";

		Loading->Visibility = ::Visibility::Visible;
		Stats->Visibility = ::Visibility::Collapsed;
		LevelStart->Visibility = ::Visibility::Collapsed;
		PauseData->Visibility = ::Visibility::Collapsed;
		LoadingProgress->IsActive = true;
	})
		);
}

void ScenePanel::SetGameStats(
	int maxLevel,
	int hitCount,
	int shotCount
	)
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this, maxLevel, hitCount, shotCount]()
	{
		GameInfoOverlayTitle->Text = "Game Statistics";
		//m_possiblePurchaseUpgrade = true;
		//OptionalTrialUpgrade();

		Loading->Visibility = ::Visibility::Collapsed;
		Stats->Visibility = ::Visibility::Visible;
		LevelStart->Visibility = ::Visibility::Collapsed;
		PauseData->Visibility = ::Visibility::Collapsed;

		static const int bufferLength = 20;
		static char16 wsbuffer[bufferLength];

		int length = swprintf_s(wsbuffer, bufferLength, L"%d", maxLevel);
		LevelsCompleted->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%d", hitCount);
		TotalPoints->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%d", shotCount);
		TotalShots->Text = ref new Platform::String(wsbuffer, length);

		// High Score is not used for showing Game Statistics
		HighScoreTitle->Visibility = ::Visibility::Collapsed;
		HighScoreData->Visibility = ::Visibility::Collapsed;
	})
		);
}

void ScenePanel::SetGameOver(
	bool win,
	int maxLevel,
	int hitCount,
	int shotCount,
	int highScore
	)
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this, win, maxLevel, hitCount, shotCount, highScore]()
	{
		if (win)
		{
			GameInfoOverlayTitle->Text = "You Won!";
			//m_possiblePurchaseUpgrade = true;
			//OptionalTrialUpgrade();
		}
		else
		{
			GameInfoOverlayTitle->Text = "Game Over";
			//m_possiblePurchaseUpgrade = false;
			//PurchaseUpgrade->Visibility = ::Visibility::Collapsed;
		}
		Loading->Visibility = ::Visibility::Collapsed;
		Stats->Visibility = ::Visibility::Visible;
		LevelStart->Visibility = ::Visibility::Collapsed;
		PauseData->Visibility = ::Visibility::Collapsed;

		static const int bufferLength = 20;
		static char16 wsbuffer[bufferLength];

		int length = swprintf_s(wsbuffer, bufferLength, L"%d", maxLevel);
		LevelsCompleted->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%d", hitCount);
		TotalPoints->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%d", shotCount);
		TotalShots->Text = ref new Platform::String(wsbuffer, length);

		// Show High Score
		HighScoreTitle->Visibility = ::Visibility::Visible;
		HighScoreData->Visibility = ::Visibility::Visible;
		length = swprintf_s(wsbuffer, bufferLength, L"%d", highScore);
		HighScore->Text = ref new Platform::String(wsbuffer, length);
	})
		);
}

void ScenePanel::SetLevelStart(
	int level,
	Platform::String^ objective,
	float timeLimit,
	float bonusTime
	)
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this, level, objective, timeLimit, bonusTime]()
	{
		static const int bufferLength = 20;
		static char16 wsbuffer[bufferLength];

		int length = swprintf_s(wsbuffer, bufferLength, L"Level %d", level);
		GameInfoOverlayTitle->Text = ref new Platform::String(wsbuffer, length);

		Loading->Visibility = ::Visibility::Collapsed;
		Stats->Visibility = ::Visibility::Collapsed;
		LevelStart->Visibility = ::Visibility::Visible;
		PauseData->Visibility = ::Visibility::Collapsed;

		Objective->Text = objective;

		length = swprintf_s(wsbuffer, bufferLength, L"%6.1f sec", timeLimit);
		TimeLimit->Text = ref new Platform::String(wsbuffer, length);

		if (bonusTime > 0.0)
		{
			BonusTimeTitle->Visibility = ::Visibility::Visible;
			BonusTimeData->Visibility = ::Visibility::Visible;
			length = swprintf_s(wsbuffer, bufferLength, L"%6.1f sec", bonusTime);
			BonusTime->Text = ref new Platform::String(wsbuffer, length);
		}
		else
		{
			BonusTimeTitle->Visibility = ::Visibility::Collapsed;
			BonusTimeData->Visibility = ::Visibility::Collapsed;
		}
	})
		);
}

void ScenePanel::SetPause(int level, int hitCount, int shotCount, float timeRemaining)
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this, level, hitCount, shotCount, timeRemaining]()
	{
		GameInfoOverlayTitle->Text = "Paused";
		Loading->Visibility = ::Visibility::Collapsed;
		Stats->Visibility = ::Visibility::Collapsed;
		LevelStart->Visibility = ::Visibility::Collapsed;
		PauseData->Visibility = ::Visibility::Visible;

		static const int bufferLength = 20;
		static char16 wsbuffer[bufferLength];

		int length = swprintf_s(wsbuffer, bufferLength, L"%d", level);
		PauseLevel->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%d", hitCount);
		PauseHits->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%d", shotCount);
		PauseShots->Text = ref new Platform::String(wsbuffer, length);

		length = swprintf_s(wsbuffer, bufferLength, L"%6.1f sec", timeRemaining);
		PauseTimeRemaining->Text = ref new Platform::String(wsbuffer, length);
	})
		);
}

void ScenePanel::ShowTooSmall()
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this]()
	{
		VisualStateManager::GoToState(this, "TooSmallState", true);
	})
		);
}

void ScenePanel::HideTooSmall()
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this]()
	{
		VisualStateManager::GoToState(this, "NotTooSmallState", true);
	})
		);
}

void ScenePanel::HideGameInfoOverlay()
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this]()
	{
		VisualStateManager::GoToState(this, "NormalState", true);

		//StoreFlyout->IsOpen = false;
		//StoreFlyout->Visibility = ::Visibility::Collapsed;
		//GameAppBar->IsOpen = false;
		//Play->Content = "Pause";
		//Play->Icon = ref new SymbolIcon(Symbol::Pause);
		m_playActive = true;
	})
		);
}

void ScenePanel::SetAction(GameInfoOverlayCommand action)
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this, action]()
	{
		// Enable only one of the four possible commands at the bottom of the
		// Game Info Overlay.

		PlayAgain->Visibility = ::Visibility::Collapsed;
		PleaseWait->Visibility = ::Visibility::Collapsed;
		TapToContinue->Visibility = ::Visibility::Collapsed;

		switch (action)
		{
		case GameInfoOverlayCommand::PlayAgain:
			PlayAgain->Visibility = ::Visibility::Visible;
			break;
		case GameInfoOverlayCommand::PleaseWait:
			PleaseWait->Visibility = ::Visibility::Visible;
			break;
		case GameInfoOverlayCommand::TapToContinue:
			TapToContinue->Visibility = ::Visibility::Visible;
			break;
		case GameInfoOverlayCommand::None:
			break;
		}
	})
		);
}

void ScenePanel::ShowGameInfoOverlay()
{
	// This function may be called from a different thread.
	// All XAML updates need to occur on the UI thread so dispatch to ensure this is true.
	Dispatcher->RunAsync(
		CoreDispatcherPriority::Normal,
		ref new DispatchedHandler([this]()
	{
		VisualStateManager::GoToState(this, "GameInfoOverlayState", true);
		//Play->Content = "Play";
		//Play->Icon = ref new SymbolIcon(Symbol::Play);
		m_playActive = false;
	})
		);
}

//-----------Buttons----------------------

void ScenePanel::OnChangeBackgroundButtonClicked(Object^ /* sender */, RoutedEventArgs^ /* args */)
{
	m_main->CycleBackground();
}

void ScenePanel::OnCompositionScaleChanged(Windows::UI::Xaml::Controls::SwapChainPanel ^sender, Platform::Object ^args)
{
	critical_section::scoped_lock lock(m_main->GetCriticalSection());
	m_deviceResources->SetCompositionScale(sender->CompositionScaleX, sender->CompositionScaleY);
	m_main->CreateWindowSizeDependentResources();
}

void ScenePanel::OnSwapChainPanelSizeChanged(Platform::Object^ sender, Windows::UI::Xaml::SizeChangedEventArgs^ e)
{
	critical_section::scoped_lock lock(m_main->GetCriticalSection());
	m_deviceResources->SetLogicalSize(e->NewSize);
	m_main->CreateWindowSizeDependentResources();
}

void ScenePanel::SetYawPitch(float yaw, float pitch)
{
	m_main->SetYawPitch(yaw, pitch);
}

void ScenePanel::Fire()
{
	m_main->Fire();
}