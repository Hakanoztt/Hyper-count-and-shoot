#import <Foundation/Foundation.h>
#import "UnityAppController.h"


@interface IdfaConsentViewController : UnityAppController

@property (nonatomic, retain) UIView *frame;

+ (IdfaConsentViewController*)sharedInstance;

- (void) showForceUpdate:(NSString*) message
                        :(NSString*) title;
- (void) showAlertDialog:(NSString*) titleText
                        :(NSString*) descriptionText;
- (void) presentConsentView;
- (void) presentConsentView2;
- (void) presentConsentView3:(NSString*) titleText
                            :(int) delay
                            :(int) position
                            :(NSString*) descriptionText
                            :(NSString*) buttonText
                            :(NSString*) termsText
                            :(NSString*) policyText
                            :(NSString*) termsUrl
                            :(NSString*) policyUrl;
- (void) requstPermissionForAppTracking;
- (UIView*) getRootView;


@end



