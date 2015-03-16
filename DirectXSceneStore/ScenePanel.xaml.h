//
// ScenePanel.xaml.h
// Declaration of the ScenePanel class
//

#pragma once

#include "ScenePanel.g.h"
#include "Common\DeviceResources.h"
#include "GameMain.h"
#include "ProductItem.h"

namespace DirectXSceneStore
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class ScenePanel sealed : IGameUIControl
	{
	public:
		ScenePanel();
		
		void OnSuspending();
		void OnResuming();

		// Exposing control methods
		void SetYawPitch(float yaw, float pitch);
		void Fire();

		// IGameUIControl methods.
		virtual void SetAction(GameInfoOverlayCommand action);
		virtual void SetGameLoading();
		virtual void SetGameStats(int maxLevel, int hitCount, int shotCount);
		virtual void SetGameOver(bool win, int maxLevel, int hitCount, int shotCount, int highScore);
		virtual void SetLevelStart(int level, Platform::String^ objective, float timeLimit, float bonusTime);
		virtual void SetPause(int level, int hitCount, int shotCount, float timeRemaining);
		virtual void ShowTooSmall();
		virtual void HideTooSmall();
		virtual void HideGameInfoOverlay();
		virtual void ShowGameInfoOverlay();

	private:
		std::shared_ptr<DX::DeviceResources> m_deviceResources;
		std::unique_ptr<GameMain> m_main;
		bool m_playActive;

		void OnGameInfoOverlayTapped(Object^ sender, Windows::UI::Xaml::Input::TappedRoutedEventArgs^ args);

		void OnChangeBackgroundButtonClicked(Object^ sender, Windows::UI::Xaml::RoutedEventArgs^ args);

		void OnCompositionScaleChanged(Windows::UI::Xaml::Controls::SwapChainPanel ^sender, Platform::Object ^args);
		void OnSwapChainPanelSizeChanged(Platform::Object^ sender, Windows::UI::Xaml::SizeChangedEventArgs^ e);
	};
}
